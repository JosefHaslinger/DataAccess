﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JPB.DataAccess.Config.Model;
using JPB.DataAccess.Contacts;
using JPB.DataAccess.ModelsAnotations;

namespace JPB.DataAccess.Config
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ConfigurationResolver<T>
	{
		internal ConfigurationResolver()
		{
			
		}

		private Config _config;

		/// <summary>
		/// Easy access to the known Class Info
		/// </summary>
		public ClassInfoCache ClassInfoCache { get; private set; }

		internal ConfigurationResolver(Config config, ClassInfoCache classInfoCache)
		{
			ClassInfoCache = classInfoCache;
			this._config = config;
		}

		/// <summary>
		/// Set a attribute on a property
		/// </summary>
		/// <typeparam name="TProp"></typeparam>
		/// <param name="exp"></param>
		/// <param name="attribute"></param>
		public void SetPropertyAttribute<TProp>(Expression<Func<T, TProp>> exp, DataAccessAttribute attribute)
		{
			var info = ConfigHelper.GetPropertyInfoFromLabda(exp);
			var fod = ClassInfoCache.PropertyInfoCaches.First(s => s.Key == info).Value;
			fod.AttributeInfoCaches.Add(new AttributeInfoCache(attribute));
		}
		
		/// <summary>
		/// Set a attribute on a property
		/// </summary>
		/// <typeparam name="TProp"></typeparam>
		/// <param name="info"></param>
		/// <param name="attribute"></param>
		public void SetPropertyAttribute(string info, DataAccessAttribute attribute)
		{
			var fod = ClassInfoCache.PropertyInfoCaches.First(s => s.Key == info).Value;
			fod.AttributeInfoCaches.Add(new AttributeInfoCache(attribute));
		}

		/// <summary>
		/// set a Attribute on a method
		/// </summary>
		/// <typeparam name="TProp"></typeparam>
		/// <param name="exp"></param>
		/// <param name="attribute"></param>
		public void SetMethodAttribute<TProp>(Expression<Func<T, TProp>> exp, DataAccessAttribute attribute)
		{
			var info = ConfigHelper.GetMehtodInfoFromLabda(exp);
			var fod = ClassInfoCache.MethodInfoCaches.First(s => s.MethodName == info);
			fod.AttributeInfoCaches.Add(new AttributeInfoCache(attribute));
		}


		/// <summary>
		/// set a Attribute on a method
		/// </summary>
		/// <typeparam name="TProp"></typeparam>
		/// <param name="info"></param>
		/// <param name="attribute"></param>
		public void SetMethodAttribute(string info, DataAccessAttribute attribute)
		{
			var fod = ClassInfoCache.MethodInfoCaches.First(s => s.MethodName == info);
			fod.AttributeInfoCaches.Add(new AttributeInfoCache(attribute));
		}

		/// <summary>
		/// Adds a Fake Mehtod to the class
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="methodBody"></param>
		/// <param name="attributes"></param>
		public void CreateMethod(string methodName, Delegate methodBody, params AttributeInfoCache[] attributes)
		{
			if (methodName == null)
				throw new ArgumentNullException("methodName");
			if (methodBody == null)
				throw new ArgumentNullException("methodBody");
			if (ClassInfoCache.MethodInfoCaches.Any(s => s.MethodName == methodName))
				throw new ArgumentOutOfRangeException("methodName", "Method name does exist. Cannot define a Method twice");
			var mehtodInfo = new MethodInfoCache(methodBody, methodName, attributes);
			ClassInfoCache.MethodInfoCaches.Add(mehtodInfo);
		}

		/// <summary>
		/// Adds a Fake property to the class getter and setter will be invoked like normal ones
		/// </summary>
		/// <typeparam name="TE"></typeparam>
		/// <param name="name"></param>
		/// <param name="setter"></param>
		/// <param name="getter"></param>
		/// <param name="attributes"></param>
		public void CreateProperty<TE>(string name, Action<T, TE> setter = null, Func<T, TE> getter = null, params AttributeInfoCache[] attributes)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			if (ClassInfoCache.PropertyInfoCaches.Any(s => s.Key == name))
				throw new ArgumentOutOfRangeException("name", "Property name does exist. Cannot define a property twice");

			if (setter == null && getter == null)
				throw new ArgumentNullException("setter", "Propertys must define at least one accessor. You cannot define a property without getter and setter");

			var propInfo = new PropertyInfoCache<T, TE>(name, setter, getter, attributes);
			ClassInfoCache.PropertyInfoCaches.Add(name, propInfo);
		}

		/// <summary>
		/// set a Attribute on a class
		/// </summary>
		/// <param name="attribute"></param>
		public void SetClassAttribute(DataAccessAttribute attribute)
		{
			ClassInfoCache.AttributeInfoCaches.Add(new AttributeInfoCache(attribute));
		}

		/// <summary>
		/// Set a Mehtod for creating an instance. When FullLoad is true the Framework assumes that the Factory has loaded all infos from the IDataRecord into the new Object
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="fullLoad"></param>
		public void SetFactory(Func<IDataRecord, object> factory, bool fullLoad)
		{
			ClassInfoCache.Factory = factory;
			ClassInfoCache.FullFactory = fullLoad;
		}

		/// <summary>
		/// Set a converter type that allows you to convert incomming and outgoing data to be converted befor set to the property
		/// </summary>
		/// <typeparam name="TProp"></typeparam>
		/// <param name="exp"></param>
		/// <param name="converter"></param>
		public void SetConverter<TProp>(Expression<Func<T, TProp>> exp, Type converter)
		{
			SetPropertyAttribute(exp, new ValueConverterAttribute(converter));
		}

		//class RuntimeConverter : IValueConverter
		//{
		//	private readonly Func<object, Type, object, CultureInfo, object> _convertFrom;
		//	private readonly Func<object, Type, object, CultureInfo, object> _convertTo;


		//	public RuntimeConverter(Func<object, Type, object, CultureInfo, object> convertFrom, 
		//		Func<object, Type, object, CultureInfo, object> convertTo)
		//	{
		//		_convertFrom = convertFrom;
		//		_convertTo = convertTo;
		//	}

		//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		//	{
		//		return _convertFrom(value, targetType, parameter, culture);
		//	}

		//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		//	{
		//		return _convertTo(value, targetType, parameter, culture);
		//	}
		//}

		//public void SetConverter<TProp>(Expression<Func<T, TProp>> exp,
		//	Func<object, Type, object, CultureInfo, object> convertFrom,
		//	Func<object, Type, object, CultureInfo, object> convertTo, object key)
		//{
		//	SetPropertyAttribute(exp, new ValueConverterAttribute(new RuntimeConverter(convertFrom, convertTo), key));
		//}

		/// <summary>
		/// Set the Primary key 
		/// </summary>
		/// <param name="exp"></param>
		public void SetPrimaryKey<TProp>(Expression<Func<T, TProp>> exp)
		{
			SetPropertyAttribute(exp, new PrimaryKeyAttribute());
		}

		/// <summary>
		/// Set a Forgin key on a Property
		/// </summary>
		/// <param name="exp"></param>
		public void SetForginKey<TProp>(Expression<Func<T, TProp>> exp)
		{
			SetPropertyAttribute(exp, new ForeignKeyAttribute());
		}

		/// <summary>
		/// Set a ForModel key on a Property
		/// </summary>
		/// <param name="exp"></param>
		public void SetForModelKey<TProp>(Expression<Func<T, TProp>> exp, string value)
		{
			SetPropertyAttribute(exp, new ForModel(value));
		}

		/// <summary>
		/// Set a delete factory mehtod
		/// </summary>
		/// <param name="exp"></param>
		public void SetDeleteFactoryKey<TProp>(Expression<Func<T, TProp>> exp)
		{
			SetMethodAttribute(exp, new DeleteFactoryMethodAttribute());
		}

		/// <summary>
		/// Set a Object factory mehtod
		/// </summary>
		/// <param name="exp"></param>
		public void SetObjectFactoryKey<TProp>(Expression<Func<T, TProp>> exp)
		{
			SetMethodAttribute(exp, new ObjectFactoryMethodAttribute());
		}

		/// <summary>
		/// Set a Select method
		/// </summary>
		/// <param name="exp"></param>
		public void SetSelectFactoryKey<TProp>(Expression<Func<T, TProp>> exp)
		{
			SetMethodAttribute(exp, new SelectFactoryMethodAttribute());
		}

		/// <summary>
		/// Set a Insert mehtod
		/// </summary>
		/// <param name="exp"></param>
		public void SetInsertFactoryKey<TProp>(Expression<Func<T, TProp>> exp)
		{
			SetMethodAttribute(exp, new InsertFactoryMethodAttribute());
		}

		/// <summary>
		/// Set a Update mehtod
		/// </summary>
		/// <param name="exp"></param>
		public void SetUpdateFactoryKey<TProp>(Expression<Func<T, TProp>> exp)
		{
			SetMethodAttribute(exp, new UpdateFactoryMethodAttribute());
		}


		/// <summary>
		/// Set the Table Name ForModel key
		/// </summary>
		/// <param name="exp"></param>
		public void SetTableNameKey<TProp>(Expression<Func<T, TProp>> exp, string name)
		{
			SetClassAttribute(new ForModel(name));
		}

		///// <summary>
		///// Set the Primary key 
		///// </summary>
		///// <param name="config"></param>
		///// <param name="PropertyName"></param>
		//public void SetStaticQueryKey(string query)
		//{

		//}
	}
}
