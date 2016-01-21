﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JPB.DataAccess.Config.Contract;

namespace JPB.DataAccess.Config.Model
{
	[DebuggerDisplay("{PropertyName}")]
	[Serializable]
	internal class PropertyHelper : MethodInfoCache
	{
		private dynamic _getter;
		private dynamic _setter;

		public void SetGet(dynamic getter)
		{
			_getter = getter;
		}

		public void SetSet(dynamic setter)
		{
			_setter = setter;
		}

		public override object Invoke(dynamic target, params dynamic[] param)
		{
			if (_getter != null)
			{
				return _getter(target);
			}
			var paramOne = param[0];
			_setter(target, paramOne);
			return null;
		}
	}

	/// <summary>
	///     Infos about the Property
	/// </summary>
	[DebuggerDisplay("{PropertyName}")]
	[Serializable]
	public class PropertyInfoCache : IPropertyInfoCache
	{
		/// <summary>
		/// </summary>
		internal PropertyInfoCache(PropertyInfo propertyInfo, bool anon)
		{
			Init(propertyInfo, anon);
		}

		public PropertyInfoCache()
		{
			AttributeInfoCaches = new HashSet<AttributeInfoCache>();
		}

		public virtual IPropertyInfoCache Init(PropertyInfo propertyInfo, bool anon)
		{
			AttributeInfoCaches = new HashSet<AttributeInfoCache>();
			if (propertyInfo != null)
			{
				var getMethod = propertyInfo.GetGetMethod();
				var setMethod = propertyInfo.GetSetMethod();
				PropertyInfo = propertyInfo;
				PropertyName = propertyInfo.Name;
				PropertyType = propertyInfo.PropertyType;

				GetterDelegate = typeof(Func<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
				SetterDelegate = typeof(Action<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
				if (!anon)
				{
					var builder = typeof(Expression)
						.GetMethods()
						.First(s => s.Name == "Lambda" && s.ContainsGenericParameters);

					var thisRef = Expression.Parameter(propertyInfo.DeclaringType, "that");

					var accessField = Expression.MakeMemberAccess(thisRef, propertyInfo);

					if (getMethod != null && getMethod.IsPublic)
					{
						var getExpression = builder
							.MakeGenericMethod(GetterDelegate)
							.Invoke(null, new object[]
						{
							accessField,
							new[] {thisRef}
						}) as dynamic;

						var getterDelegate = getExpression.Compile();
						Getter = new PropertyHelper();
						((PropertyHelper)Getter).SetGet(getterDelegate);
					}
					if (setMethod != null && setMethod.IsPublic)
					{
						var valueRef = Expression.Parameter(propertyInfo.PropertyType, "newValue");
						var setter = Expression.Assign(
							accessField,
							valueRef);

						var setExpression = builder
							.MakeGenericMethod(SetterDelegate)
							.Invoke(null, new object[]
						{
							setter,
							new[] {thisRef, valueRef}
						}) as dynamic;

						var setterDelegate = setExpression.Compile();
						Setter = new PropertyHelper();
						((PropertyHelper)Setter).SetSet(setterDelegate);
					}
				}
				else
				{
					if (getMethod != null)
						Getter = new MethodInfoCache(getMethod);
					if (setMethod != null)
						Setter = new MethodInfoCache(setMethod);
				}

				AttributeInfoCaches = new HashSet<AttributeInfoCache>(propertyInfo
					.GetCustomAttributes(true)
					.Where(s => s is Attribute)
					.Select(s => new AttributeInfoCache(s as Attribute)));
			}

			return this;
		}

		/// <summary>
		///     the type of the Setter delegate
		/// </summary>
		public Type SetterDelegate { get; protected internal set; }

		/// <summary>
		///     the type of the Getter delegate
		/// </summary>
		public Type GetterDelegate { get; protected internal set; }

		/// <summary>
		///     The Setter mehtod can be null
		/// </summary>
		public MethodInfoCache Setter { get; protected internal set; }

		/// <summary>
		///     The Getter Method can be null
		/// </summary>
		public MethodInfoCache Getter { get; protected internal set; }

		/// <summary>
		///     The return type of the property
		/// </summary>
		public Type PropertyType { get; protected internal set; }

		/// <summary>
		///     Direct Reflection
		/// </summary>
		public PropertyInfo PropertyInfo { get; protected internal set; }

		//public ClassInfoCache<PropertyInfoCache, AttributeInfoCache, MethodInfoCache, ConstructorInfoCache> PropertyTypeInfo { get; set; }

		/// <summary>
		///     The name of the Property
		/// </summary>
		public string PropertyName { get; protected internal set; }

		/// <summary>
		///     All Attributes on this Property
		/// </summary>
		public HashSet<AttributeInfoCache> AttributeInfoCaches { get; protected internal set; }


		public int CompareTo(PropertyInfoCache other)
		{
			return GetHashCode() - other.GetHashCode();
		}

		public override int GetHashCode()
		{
			return PropertyName.GetHashCode();
		}

		// returns property getter
		internal static Delegate GetPropGetter(Type delegateType, Type typeOfObject, string propertyName)
		{
			var paramExpression = Expression.Parameter(typeOfObject, "value");
			var propertyGetterExpression = Expression.Property(paramExpression, propertyName);
			return Expression.Lambda(delegateType, propertyGetterExpression, paramExpression).Compile();
		}

		// returns property setter:
		internal static Delegate GetPropSetter(Type delegateType, Type typeOfObject, Type typeOfProperty, string propertyName)
		{
			var paramExpression = Expression.Parameter(typeOfObject);
			var paramExpression2 = Expression.Parameter(typeOfProperty, propertyName);
			var propertyGetterExpression = Expression.Property(paramExpression, propertyName);
			return
				Expression.Lambda(delegateType, Expression.Assign(propertyGetterExpression, paramExpression2), paramExpression,
					paramExpression2)
					.Compile();
		}
	}

	[Serializable]
	internal class PropertyInfoCache<T, TE> : PropertyInfoCache
	{
		internal PropertyInfoCache(string name, Action<T, TE> setter = null, Func<T, TE> getter = null,
			params AttributeInfoCache[] attributes)
		{
			if (attributes == null)
				throw new ArgumentNullException("attributes");

			PropertyName = name;

			if (setter != null)
			{
				Setter = new MethodInfoCache(setter);
			}

			if (getter != null)
			{
				Getter = new MethodInfoCache(getter);
			}
		}
	}
}