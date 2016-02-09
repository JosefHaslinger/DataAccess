/*
This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/.
Please consider to give some Feedback on CodeProject

http://www.codeproject.com/Articles/818690/Yet-Another-ORM-ADO-NET-Wrapper

*/
using System;
using System.Linq.Expressions;
using System.Reflection;
#if !DEBUG
using System.Diagnostics;
#endif
using JPB.DataAccess.DbInfoConfig.DbInfo;

namespace JPB.DataAccess.MetaApi
{
	/// <summary>
	/// Extention methods for easy access to meta infos
	/// </summary>
	public static class MetaInfoStoreExtentions
	{
		/// <summary>
		///     Anonymous type check by naming convention
		/// </summary>
		/// <returns></returns>
		internal static Boolean IsAnonymousType(this Type type)
		{
			//http://stackoverflow.com/questions/1650681/determining-whether-a-type-is-an-anonymous-type
			return type.Namespace == null;
		}
		
		public static string GetPropertyInfoFromLabda<TSource, TProperty>(
			Expression<Func<TSource, TProperty>> propertyLambda)
		{
			var type = typeof(TSource);

			var member = propertyLambda.Body as MemberExpression;
			if (member == null)
				throw new ArgumentException(String.Format(
					"Expression '{0}' refers to a method, not a property.",
					propertyLambda));

			var propInfo = member.Member as PropertyInfo;
			if (propInfo == null)
				throw new ArgumentException(String.Format(
					"Expression '{0}' refers to a field, not a property.",
					propertyLambda));

			return propInfo.Name;
		}

		public static string GetMehtodInfoFromLabda<TSource, TProperty>(
			Expression<Func<TSource, TProperty>> propertyLambda)
		{
			var type = typeof(TSource);

			var member = propertyLambda.Body as MemberExpression;
			if (member != null)
				throw new ArgumentException(String.Format(
					"Expression '{0}' refers to a property, not a method.",
					propertyLambda));

			var propInfo = member.Member as PropertyInfo;
			if (propInfo == null)
				throw new ArgumentException(String.Format(
					"Expression '{0}' refers to a field, not a property.",
					propertyLambda));

			return propInfo.Name;
		}
	}
}