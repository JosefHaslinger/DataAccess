﻿using System;
using System.Linq.Expressions;
using JPB.DataAccess.MetaApi;
using JPB.DataAccess.Query.Contracts;

namespace JPB.DataAccess.Query.Operators.Conditional
{
	public class ConditionalQuery<TPoco> : QueryBuilderX, IConditionalQuery<TPoco>
	{
		public readonly CondtionBuilderState State;

		public ConditionalQuery(IQueryBuilder queryText, CondtionBuilderState state) : base(queryText)
		{
			State = state;
		}

		public ConditionalQuery(ConditionalQuery<TPoco> queryText) : base(queryText)
		{
			State = queryText.State;
		}

		/// <summary>
		/// Opens a new Logical combined Query
		/// </summary>
		/// <returns></returns>
		public ConditionalQuery<TPoco> Parenthesis()
		{
			return new ConditionalQuery<TPoco>(this.QueryText("("), State.ToInBreaket(true));
		}

		/// <summary>
		///     Adds a SQL WHERE statement
		///		does not emit any conditional statement
		///		should be followed by Column()
		/// </summary>
		/// <returns></returns>
		public ConditionalQuery<TPoco> Alias(string alias)
		{
			return new ConditionalQuery<TPoco>(this, State.ToAlias(alias));
		}

		/// <summary>
		/// Prepaires an Conditional Query that targets an single Column
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="columnName"></param>
		/// <returns></returns>
		public ConditionalColumnQuery<TPoco> Column(string columnName)
		{
			return new ConditionalColumnQuery<TPoco>(this.QueryText(columnName), State);
		}

		/// <summary>
		/// Prepaires an Conditional Query that targets an single Column
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="columnName"></param>
		/// <returns></returns>
		public ConditionalColumnQuery<TPoco> Column<TA>(
								Expression<Func<TPoco, TA>> columnName)
		{
			var member = columnName.GetPropertyInfoFromLabda();
			var propName = this.ContainerObject.AccessLayer.GetClassInfo(typeof(TPoco)).Propertys[member];
			if (CurrentIdentifier != null)
				return Column(CurrentIdentifier + "." + propName.DbName);
			return Column(propName.DbName);
		}

		public string CurrentIdentifier
		{
			get { return this.State.Identifier; }
		}
	}
}
