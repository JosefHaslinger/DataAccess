using System;
using System.Linq.Expressions;
using System.Reflection;
using JPB.DataAccess.DbInfoConfig;
using JPB.DataAccess.DbInfoConfig.DbInfo;
using JPB.DataAccess.Helper.LocalDb.Constraints.Contracts;

namespace JPB.DataAccess.Helper.LocalDb.Constraints.Defaults
{
	public class LocalDbDefaultConstraintEx<TEntity, TValue> : ILocalDbDefaultConstraint<TEntity>
	{
		private readonly DbConfig _config;
		private readonly Func<TValue> _generateValue;
		private readonly Expression<Func<TEntity, TValue>> _exp;
		private DbPropertyInfoCache _dbPropertyInfoCache;

		public LocalDbDefaultConstraintEx(DbConfig config, string name, Func<TValue> generateValue, Expression<Func<TEntity, TValue>> column)
		{
			_config = config;
			_generateValue = generateValue;
			_exp = column;
			Name = name;

			var member = _exp.Body as MemberExpression;
			if (member == null)
				throw new ArgumentException(String.Format(
					"Expression '{0}' refers to a method, not a property.",
					_exp));

			var propInfo = member.Member as PropertyInfo;
			if (propInfo == null)
				throw new ArgumentException(String.Format(
					"Expression '{0}' refers to a field, not a property.",
					_exp));

			var type = _config.GetOrCreateClassInfoCache(typeof(TEntity));
			_dbPropertyInfoCache = type.Propertys[propInfo.Name];
		}

		public void DefaultValue(TEntity item)
		{
			var value = _generateValue();
			var preValue = _dbPropertyInfoCache.Getter.Invoke(item);
			var defaultValue = default(TValue);
			if ((defaultValue == null && preValue == null) || (defaultValue != null && defaultValue.Equals(preValue)))
			{
				_dbPropertyInfoCache.Setter.Invoke(item, value);
			}
		}

		public string Name { get; private set; }
	}
}