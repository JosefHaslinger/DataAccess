using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JPB.DataAccess.Config.Model;
#if !DEBUG
using System.Diagnostics;
#endif
namespace JPB.DataAccess.Config
{
	/// <summary>
	/// Class info Storage
	/// </summary>
#if !DEBUG
	[DebuggerStepThrough]
#endif
	public class Config
	{
		static Config()
		{
			SClassInfoCaches = new List<ClassInfoCache>();
		}

		/// <summary>
		/// Creates a new Instance for configuration
		/// </summary>
		/// <param name="enableReflection">If set reflection will be used to enumerate all used class instances [Not used]</param>
		public Config(bool enableReflection = true)
		{
			this.UseReflection = enableReflection;
		}

		/// <summary>
		/// Allows you to alter the Config store that holds T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="validator"></param>
		public void SetConfig<T>(Action<ConfigurationResolver<T>> validator)
		{
			validator(new ConfigurationResolver<T>(this));
			GetOrCreateClassInfoCache(typeof(T)).RenumeratePropertys(true);
		}

		/// <summary>
		/// Indicates the usage of Reflection
		/// </summary>
		public bool UseReflection { get; private set; }

		/// <summary>
		/// Gets an Cache object if exists or creats one
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal PropertyInfoCache GetOrCreatePropertyInfoCache(PropertyInfo type)
		{
			var declareingType = type.ReflectedType;
			var name = type.Name;
			var element = SClassInfoCaches.FirstOrDefault(s => s.Type == declareingType && s.PropertyInfoCaches.Any(e => e.PropertyName == name));

			if (element == null)
			{
				var declaringType = type.ReflectedType;
				SClassInfoCaches.Add(element = new ClassInfoCache(declaringType));
				element.CheckForConfig();
			}

			return element.PropertyInfoCaches.FirstOrDefault(s => s.PropertyName == type.Name);
		}

		/// <summary>
		/// Gets an Cache object if exists or creats one
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal ClassInfoCache GetOrCreateClassInfoCache(Type type)
		{
			var element = SClassInfoCaches.FirstOrDefault(s => s.ClassName == type.FullName);
			if (element == null)
			{
				SClassInfoCaches.Add(element = new ClassInfoCache(type));
				element.CheckForConfig();
			}

			return element;
		}

		/// <summary>
		/// Gets an Cache object if exists or creats one
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal MethodInfoCache GetOrCreateMethodInfoCache(MethodInfo type)
		{
			var declareingType = type.ReflectedType;
			var name = type.Name;
			var element = SClassInfoCaches.FirstOrDefault(s => s.Type == declareingType && s.MethodInfoCaches.Any(e => e.MethodName == name));
			if (element == null)
			{
				var declaringType = type.ReflectedType;
				SClassInfoCaches.Add(element = new ClassInfoCache(declaringType));
				element.CheckForConfig();
			}

			return element.MethodInfoCaches.FirstOrDefault(s => s.MethodName == type.Name);
		}

		internal static List<ClassInfoCache> SClassInfoCaches { get; private set; }
	}
}