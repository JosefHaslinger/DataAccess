﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPB.DataAccess.Manager;
using JPB.DataAccess.Query;
using JPB.DataAccess.Tests.Base.TestModels.CheckWrapperBaseTests;
using JPB.DataAccess.Tests.Overwrite;
using NUnit.Framework;
using Users = JPB.DataAccess.Tests.Base.Users;

namespace JPB.DataAccess.Tests.QueryBuilderTests
#if MsSql
.MsSQL
#endif

#if SqLite
.SqLite
#endif
{
	[TestFixture]
	public class QueryBuilderTests
	{
		public QueryBuilderTests()
		{
			
		}

		[SetUp]
		public void Init()
		{
			DbAccessLayer = new Manager().GetWrapper();
			DataMigrationHelper.ClearDb(DbAccessLayer);
		}

		public DbAccessLayer DbAccessLayer { get; set; }

		[Test]
		public void Select()
		{
			DataMigrationHelper.AddUsers(250, DbAccessLayer);

			int runPrimetivSelect = -1;
			if (DbAccessLayer.DbAccessType == DbAccessType.MsSql)
			{
				runPrimetivSelect = DbAccessLayer.RunPrimetivSelect<int>(string.Format("SELECT COUNT(1) FROM {0}", UsersMeta.TableName))[0];
			}
			if (DbAccessLayer.DbAccessType == DbAccessType.SqLite)
			{
				runPrimetivSelect = (int)DbAccessLayer.RunPrimetivSelect<long>(string.Format("SELECT COUNT(1) FROM {0}", UsersMeta.TableName))[0];
			}
			var deSelect = DbAccessLayer.Select<Users>();
			var forResult = DbAccessLayer.Query().Select<Users>().ForResult().ToArray();

			Assert.That(runPrimetivSelect, Is.EqualTo(forResult.Count()));
			Assert.That(deSelect.Length, Is.EqualTo(forResult.Count()));

			for (int index = 0; index < forResult.Length; index++)
			{
				var userse = forResult[index];
				var userbe = deSelect[index];
				Assert.That(userse.UserID, Is.EqualTo(userbe.UserID));
				Assert.That(userse.UserName, Is.EqualTo(userbe.UserName));
			}
		}

		[Test]
		public void Count()
		{
			DataMigrationHelper.AddUsers(250, DbAccessLayer);

			int runPrimetivSelect = -1;
			int forResult = -1;
			var deSelect = DbAccessLayer.Select<Users>();
			if (DbAccessLayer.DbAccessType == DbAccessType.MsSql)
			{
				runPrimetivSelect = DbAccessLayer.RunPrimetivSelect<int>(string.Format("SELECT COUNT(1) FROM {0}", UsersMeta.TableName))[0];
				forResult = DbAccessLayer.Query().Select<Users>().CountInt().ForResult().FirstOrDefault();
			}
			if (DbAccessLayer.DbAccessType == DbAccessType.SqLite)
			{
				runPrimetivSelect = (int)DbAccessLayer.RunPrimetivSelect<long>(string.Format("SELECT COUNT(1) FROM {0}", UsersMeta.TableName))[0];
				forResult = (int)DbAccessLayer.Query().Select<Users>().CountLong().ForResult().FirstOrDefault();
			}

			Assert.That(runPrimetivSelect, Is.EqualTo(forResult));
			Assert.That(deSelect.Length, Is.EqualTo(forResult));
		}

		[Test]
		public void In()
		{
			var addBooksWithImage = DataMigrationHelper.AddBooksWithImage(250,2, DbAccessLayer);
			foreach (var id in addBooksWithImage)
			{
				int countOfImages = -1;
				if (DbAccessLayer.DbAccessType == DbAccessType.MsSql)
				{
					countOfImages =
					DbAccessLayer.RunPrimetivSelect<int>(string.Format("SELECT COUNT(1) FROM {0} WHERE {0}.{1} = {2}",
					ImageMeta.TableName, ImageMeta.ForgeinKeyName,
					id))[0];
				}
				if (DbAccessLayer.DbAccessType == DbAccessType.SqLite)
				{
					countOfImages =
					(int)DbAccessLayer.RunPrimetivSelect<long>(string.Format("SELECT COUNT(1) FROM {0} WHERE {0}.{1} = {2}",
					ImageMeta.TableName, ImageMeta.ForgeinKeyName,
					id))[0];
				}
				
				Assert.That(countOfImages, Is.EqualTo(2));
				var deSelect = DbAccessLayer.SelectNative<Image>(string.Format("{2} AS b WHERE b.{0} = {1}", ImageMeta.ForgeinKeyName, id, ImageMeta.SelectStatement));
				Assert.That(deSelect, Is.Not.Empty);
				Assert.That(deSelect.Length, Is.EqualTo(countOfImages));
				var book = DbAccessLayer.Select<Book>(id);
				var forResult = DbAccessLayer.Query().Select<Image>().In(book).ForResult().ToArray();
				Assert.That(forResult, Is.Not.Empty);
				Assert.That(forResult.Count, Is.EqualTo(countOfImages));
			}
		}
		[Category("MsSQL")]
#if SqLite
		[Ignore("MsSQL only")]
#endif
		[Test]
		public void Pager()
		{
			var maxItems = 250;

			DataMigrationHelper.AddUsers(maxItems, DbAccessLayer);


			var basePager = DbAccessLayer.Database.CreatePager<Users>();
			basePager.PageSize = 10;
			basePager.LoadPage(DbAccessLayer);

			Assert.That(basePager.CurrentPage, Is.EqualTo(1));
			Assert.That(basePager.MaxPage, Is.EqualTo(maxItems / basePager.PageSize));

			var queryPager = DbAccessLayer.Query().Select<Users>().ForPagedResult();
			queryPager.LoadPage(DbAccessLayer);

			Assert.That(basePager.CurrentPage, Is.EqualTo(queryPager.CurrentPage));
			Assert.That(basePager.MaxPage, Is.EqualTo(queryPager.MaxPage));
		}

		[Category("MsSQL")]
#if SqLite
		[Ignore("MsSQL only")]
#endif
		[Test]
		public void PagerWithCondtion()
		{
			var maxItems = 250;
			DataMigrationHelper.AddUsers(maxItems, DbAccessLayer);


			var basePager = DbAccessLayer.Database.CreatePager<Users>();
			basePager.BaseQuery = DbAccessLayer.CreateSelect<Users>(" WHERE User_ID < 25");
			basePager.PageSize = 10;
			basePager.LoadPage(DbAccessLayer);

			Assert.That(basePager.CurrentPage, Is.EqualTo(1));
			Assert.That(basePager.MaxPage, Is.EqualTo(Math.Ceiling(25F / basePager.PageSize)));

			var queryPager = DbAccessLayer.Query().Select<Users>().Where().Column(f => f.UserID).IsQueryOperatorValue("< 25").ForPagedResult();
			queryPager.LoadPage(DbAccessLayer);

			Assert.That(basePager.CurrentPage, Is.EqualTo(queryPager.CurrentPage));
			Assert.That(basePager.MaxPage, Is.EqualTo(queryPager.MaxPage));
		}


		[Category("MsSQL")]
#if SqLite
		[Ignore("MsSQL only")]
#endif
		[Test]
		public void AsCte()
		{
			var maxItems = 250;
			DataMigrationHelper.AddUsers(maxItems, DbAccessLayer);
			var elementProducer = DbAccessLayer.Query().Select<Users>().AsCte<Users, Users>("cte");
			var query = elementProducer.ContainerObject.Compile();
		}
	}
}
