﻿using System;
using System.Collections.Generic;
using System.Linq;
using JPB.DataAccess.DbCollection;
using JPB.DataAccess.Query;
using JPB.DataAccess.DbInfoConfig;
using JPB.DataAccess.Helper;
using JPB.DataAccess.Manager;
using JPB.DataAccess.ModelsAnotations;
using JPB.DataAccess.Tests.Base.TestModels.CheckWrapperBaseTests;
using NUnit.Framework;
using NUnit.Framework.Compatibility;
using Users = JPB.DataAccess.Tests.Base.Users;

#if SqLite
using System.Data.SQLite;
#endif

#if MsSql

#endif


namespace JPB.DataAccess.Tests.DbAccessLayerTests
#if MsSql
.MsSQL
#endif

#if SqLite
.SqLite
#endif
{
	[TestFixture]
	public class CheckWrapperBaseTests
	{
		private DbAccessLayer expectWrapper;

		[OneTimeSetUp]
		public void Init()
		{
			expectWrapper = new Manager().GetWrapper();
		}

		[SetUp]
		public void Clear()
		{
			expectWrapper.Config.Dispose();
			DbConfig.Clear();
			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);
			if (expectWrapper.DbAccessType == DbAccessType.MsSql)
				expectWrapper.ExecuteGenericCommand(string.Format("TRUNCATE TABLE {0} ", UsersMeta.TableName), null);
		}
		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertPropertyLessPoco()
		{
			Assert.That(() => expectWrapper.Insert(new UsersWithoutProperties()), Throws.Nothing);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SelectPropertyLessPoco()
		{
			InsertPropertyLessPoco();
			Assert.That(() => expectWrapper.Select<UsersWithoutProperties>(), Is.Not.Null.And.Not.Empty);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void CheckFactory()
		{
			InsertTest();
			Assert.That(() => expectWrapper.Select<Users_StaticQueryFactoryForSelect>(), Is.Not.Empty);

			var testInsertName = Guid.NewGuid().ToString();
			Users_StaticQueryFactoryForSelect testUser = null;
			Assert.That(() => testUser = expectWrapper.InsertWithSelect(new Users_StaticQueryFactoryForSelect { UserName = testInsertName }),
				Is.Not.Null
				.And.Property("UserId").Not.EqualTo(0));

			var selTestUser = expectWrapper.Select<Users_StaticQueryFactoryForSelect>(testUser.UserId);
			Assert.AreEqual(selTestUser.UserName, testUser.UserName);
			Assert.AreEqual(selTestUser.UserId, testUser.UserId);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void CheckFactoryWithArguments()
		{
			InsertTest();
			Assert.That(() => expectWrapper.Select<Users_StaticQueryFactoryForSelect>(), Is.Not.Empty);

			var testInsertName = Guid.NewGuid().ToString();
			Users_StaticQueryFactoryForSelect testUser = null;
			Assert.That(() => testUser = expectWrapper.InsertWithSelect(new Users_StaticQueryFactoryForSelect { UserName = testInsertName }),
						Is.Not.Null
						.And.Property("UserId").Not.EqualTo(0));

			var selTestUser =
				expectWrapper.Select<Users_StaticQueryFactoryForSelectWithArugments>(new object[] { testUser.UserId })
					.FirstOrDefault();
			Assert.That(selTestUser, Is.Not.Null
				.And.Property("UserName").EqualTo(testUser.UserName)
				.And.Property("UserId").EqualTo(testUser.UserId));
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void ConfigLess()
		{
			var insGuid = Guid.NewGuid().ToString();

			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);
			
			DbConfig.Clear();

			expectWrapper.Config.SetConfig<ConfigLessUser>(f =>
			{
				f.SetClassAttribute(new ForModelAttribute(UsersMeta.TableName));
				f.SetPrimaryKey(e => e.PropertyA);
				f.SetForModelKey(e => e.PropertyA, UsersMeta.PrimaryKeyName);
				f.SetForModelKey(e => e.PropertyB, UsersMeta.ContentName);
			});

			expectWrapper.Insert(new ConfigLessUser { PropertyB = insGuid });

			var elements = expectWrapper.Select<ConfigLessUser>();
			Assert.AreEqual(elements.Length, 1);

			var selectUsernameFromWhere = string.Format("SELECT UserName FROM {0}", UsersMeta.TableName);
			var selectTest = expectWrapper.Database.Run(s => s.GetSkalar(selectUsernameFromWhere));

			Assert.IsNotNull(selectTest);
			Assert.AreEqual(selectTest, insGuid);
			DbConfig.Clear();
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void ConfigLessFail()
		{
			DbConfig.Clear();
			var insGuid = Guid.NewGuid().ToString();

			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);

			expectWrapper.Config.SetConfig<ConfigLessUser>(f =>
			{
				f.SetClassAttribute(new ForModelAttribute(UsersMeta.TableName));
				f.SetPrimaryKey(e => e.PropertyA);
				f.SetForModelKey(e => e.PropertyA, UsersMeta.PrimaryKeyName + "TEST");
				f.SetForModelKey(e => e.PropertyB, UsersMeta.ContentName + "TEST");
			});

			//			var unexpected = typeof(Exception);

			//#if MsSql
			//			unexpected = typeof(SqlException);
			//#endif
			//#if SqLite
			//			unexpected = typeof(SQLiteException);
			//#endif

			Assert.That(() =>
			{
				expectWrapper.Insert(new ConfigLessUser { PropertyB = insGuid });
			}, Throws.Exception);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void AutoGenFactoryTestNullableSimple()
		{
			DbConfig.Clear();

			expectWrapper.Insert(new UsersAutoGenerateNullableConstructor());
			var elements = expectWrapper.Select<UsersAutoGenerateNullableConstructor>();
			Assert.IsNotNull(elements);
			Assert.IsNotEmpty(elements);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void GeneratedTest()
		{
			DbConfig.Clear();

			expectWrapper.Insert(new GeneratedUsers());
			var elements = expectWrapper.Select<GeneratedUsers>();
			Assert.IsNotNull(elements);
			Assert.IsNotEmpty(elements);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void AutoGenFactoryTestSimple()
		{
			DbConfig.Clear();

			expectWrapper.Insert(new UsersAutoGenerateConstructor());
			var elements = expectWrapper.Select<UsersAutoGenerateConstructor>();
			Assert.IsNotNull(elements);
			Assert.IsNotEmpty(elements);
		}

		[Test]
		[Category("MsSQL")]
		public void AutoGenFactoryTestXmlSingle()
		{
			if (expectWrapper.DbAccessType != DbAccessType.MsSql)
				return;

			DbConfig.Clear();

			expectWrapper.Insert(new UsersAutoGenerateConstructorWithSingleXml());

			var query = expectWrapper.Query()
					.QueryText("SELECT")
					.QueryText("res." + UsersMeta.PrimaryKeyName)
					.QueryText(",res." + UsersMeta.ContentName)
					.QueryText(",")
				.InBracket(s =>
				s.Select<UsersAutoGenerateConstructorWithSingleXml>()
				.ForXml(typeof(UsersAutoGenerateConstructorWithSingleXml)))
					.QueryText("AS Sub")
					.QueryText("FROM")
					.QueryText(UsersMeta.TableName)
					.QueryText("AS res");
			var elements =
				query.ForResult<UsersAutoGenerateConstructorWithSingleXml>();

			var result = elements.ToArray();

			Assert.IsNotNull(result);
			Assert.IsNotEmpty(result);
		}

		[Test()]
		[Category("MsSQL")]
		public void AutoGenFactoryTestXmlMulti()
		{
			if (expectWrapper.DbAccessType != DbAccessType.MsSql)
				return;
			DbConfig.Clear();

			expectWrapper.Insert(new UsersAutoGenerateConstructorWithMultiXml());
			expectWrapper.Insert(new UsersAutoGenerateConstructorWithMultiXml());
			expectWrapper.Insert(new UsersAutoGenerateConstructorWithMultiXml());

			var elements = expectWrapper.Query()
				.QueryText("SELECT")
				.QueryText("res." + UsersMeta.PrimaryKeyName)
				.QueryText(",res." + UsersMeta.ContentName)
				.QueryText(",")
				.InBracket(s => s.Select<UsersAutoGenerateConstructorWithMultiXml>().ForXml(typeof(UsersAutoGenerateConstructorWithMultiXml)))
				.QueryText("AS Subs")
				.QueryText("FROM")
				.QueryText(UsersMeta.TableName)
				.QueryText("AS res")
				.ForResult<UsersAutoGenerateConstructorWithMultiXml>();

			var result = elements.ToArray();

			Assert.IsNotNull(result);
			Assert.IsNotEmpty(result);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void ConfigLessInplace()
		{
			DbConfig.Clear();
			var insGuid = Guid.NewGuid().ToString();

			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);
			expectWrapper.Insert(new ConfigLessUserInplaceConfig { PropertyB = insGuid });

			var selectUsernameFromWhere = string.Format("SELECT UserName FROM {0}", UsersMeta.TableName);
			var selectTest = expectWrapper.Database.Run(s => s.GetSkalar(selectUsernameFromWhere));
			Assert.IsNotNull(selectTest);
			Assert.AreEqual(selectTest, insGuid);

			var elements = expectWrapper.Select<ConfigLessUserInplaceConfig>();
			Assert.AreEqual(elements.Length, 1);
			DbConfig.Clear();
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void ExecuteGenericCommand()
		{
			var resultSelect1 = expectWrapper.ExecuteGenericCommand("Select 10", null);
			Assert.AreEqual(resultSelect1, -1);

			resultSelect1 = expectWrapper.ExecuteGenericCommand("SELECT @test", new { test = 10 });
			Assert.AreEqual(resultSelect1, -1);

			resultSelect1 = expectWrapper.ExecuteGenericCommand("SELECT @test",
				new List<QueryParameter> { new QueryParameter("test", 10) });
			Assert.AreEqual(resultSelect1, -1);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertFactoryTest()
		{
			var insGuid = Guid.NewGuid().ToString();

			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);
			expectWrapper.IsMultiProviderEnvironment = true;
			expectWrapper.Insert(new UsersWithStaticInsert { UserName = insGuid });
			expectWrapper.IsMultiProviderEnvironment = false;
			var selectUsernameFromWhere = string.Format("SELECT UserName FROM {0}", UsersMeta.TableName);
			var selectTest = expectWrapper.Database.Run(s => s.GetSkalar(selectUsernameFromWhere));

			Assert.IsNotNull(selectTest);
			Assert.AreEqual(selectTest, insGuid);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertTest()
		{
			var insGuid = Guid.NewGuid().ToString();


			expectWrapper.Insert(new Users { UserName = insGuid });
			var selectUsernameFromWhere = string.Format("SELECT UserName FROM {0}", UsersMeta.TableName);
			var selectTest = expectWrapper.Database.Run(s => s.GetSkalar(selectUsernameFromWhere));

			Assert.IsNotNull(selectTest);
			Assert.AreEqual(selectTest, insGuid);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertRange10kTest()
		{
			var insGuid = Guid.NewGuid().ToString();
			var containingList = new List<Users>();

			for (int i = 0; i < 10000; i++)
			{
				containingList.Add(new Users() { UserName = Guid.NewGuid().ToString("N") });
			}

			var stopWatch = new Stopwatch();
			stopWatch.Start();
			expectWrapper.InsertRange(containingList);
			stopWatch.Stop();
			//Assert.That(stopWatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(7)));

			var selectUsernameFromWhere = string.Format("SELECT COUNT(1) FROM {0}", UsersMeta.TableName);
			var selectTest = expectWrapper.Database.Run(s => s.GetSkalar(selectUsernameFromWhere));

			Assert.IsNotNull(selectTest);
			Assert.AreEqual(selectTest, 10000);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertRange10kEachItemTest()
		{
			var insGuid = Guid.NewGuid().ToString();
			var containingList = new List<Users>();

			for (int i = 0; i < 10000; i++)
			{
				containingList.Add(new Users() { UserName = Guid.NewGuid().ToString("N") });
			}

			expectWrapper.RangerInsertPation = 1;
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			expectWrapper.InsertRange(containingList);
			stopWatch.Stop();
			//Assert.That(stopWatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(7)));

			var selectUsernameFromWhere = string.Format("SELECT COUNT(1) FROM {0}", UsersMeta.TableName);
			var selectTest = expectWrapper.Database.Run(s => s.GetSkalar(selectUsernameFromWhere));

			Assert.IsNotNull(selectTest);
			Assert.AreEqual(selectTest, 10000);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertDefaultValues()
		{
			new DbConfig(false)
				.Include<Users>()
				.SetConfig<Users>(conf =>
				{
					conf.SetPropertyAttribute(s => s.UserName, new IgnoreReflectionAttribute());
				});
			
			expectWrapper.Insert(new Users());
			var selectUsernameFromWhere = string.Format("SELECT UserName FROM {0}", UsersMeta.TableName);
			var selectTest = expectWrapper.Database.Run(s => s.GetSkalar(selectUsernameFromWhere));

			Assert.IsNotNull(selectTest);
			Assert.AreEqual(selectTest, DBNull.Value);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertWithSelect()
		{
			var val = new Users { UserName = "test" };
			var refSelect = expectWrapper.InsertWithSelect(val);

			Assert.AreEqual(refSelect.UserName, val.UserName);
			Assert.AreNotEqual(refSelect.UserID, val.UserID);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertWithSelectStringTest()
		{
			var insGuid = Guid.NewGuid().ToString();

			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);

			var expectedUser = expectWrapper.InsertWithSelect(new Users { UserName = insGuid });
			Assert.IsNotNull(expectedUser);
			Assert.AreEqual(expectedUser.UserName, insGuid);
			Assert.AreNotEqual(expectedUser.UserID, default(long));
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void InsertWithSelectTest()
		{
			var insGuid = Guid.NewGuid().ToString();

			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);

			var expectedUser = expectWrapper.InsertWithSelect(new Users { UserName = insGuid });
			Assert.IsNotNull(expectedUser);
			Assert.AreEqual(expectedUser.UserName, insGuid);
			Assert.AreNotEqual(expectedUser.UserID, default(long));
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void MarsTest()
		{
			RangeInsertTest();

			var baseQuery = expectWrapper.Query().Select<Users>();
			var queryA = baseQuery.ContainerObject.Compile();
			var queryB = baseQuery.ContainerObject.Compile();
			Assert.IsNotNull(queryA);
			Assert.IsNotNull(queryB);

			var marsCommand = expectWrapper.Database.MergeCommands(queryA, queryB, true);
			var returnValue = expectWrapper.ExecuteMARS(marsCommand, typeof(Users), typeof(Users));
			Assert.IsNotNull(returnValue);
			Assert.AreNotSame(returnValue.Count, 0);

			var queryAResult = returnValue.ElementAt(0);
			var queryBResult = returnValue.ElementAt(1);
			Assert.AreNotSame(queryAResult.Count, 0);
			Assert.AreEqual(queryAResult.Count, queryBResult.Count);

			var refCall = expectWrapper.Select<Users>();
			Assert.AreEqual(refCall.Length, queryAResult.Count);
		}

		[Test]
		[Category("MsSQL")]
		public void ProcedureParamLessTest()
		{
			if (expectWrapper.DbAccessType != DbAccessType.MsSql)
				return;
			RangeInsertTest();
			var expectedUser = expectWrapper.ExecuteProcedure<TestProcAParams, Users>(new TestProcAParams());

			Assert.IsNotNull(expectedUser);
			Assert.AreNotEqual(expectedUser.Length, 0);

			var refSelect =
				expectWrapper.Database.Run(s => s.GetSkalar(string.Format("SELECT COUNT (*) FROM {0}", UsersMeta.TableName)));
			Assert.AreEqual(expectedUser.Length, refSelect);
		}

		[Test]
		[Category("MsSQL")]
		public void ProcedureParamTest()
		{
			if (expectWrapper.DbAccessType != DbAccessType.MsSql)
				return;
			RangeInsertTest();

			Assert.That(() => expectWrapper.ExecuteProcedure<TestProcBParams, Users>(new TestProcBParams()
			{
				Number = 10
			}), Is.Not.Null.And.Property("Length").EqualTo(9));
		}

		[Test]
		[Category("MsSQL")]
		public void ProcedureDirectParamTest()
		{
			if (expectWrapper.DbAccessType != DbAccessType.MsSql)
				return;
			RangeInsertTest();

			Assert.That(() => expectWrapper.Select<TestProcBParamsDirect>(new object[] { 10 }),
				Is.Not.Null.And.Property("Length").EqualTo(9));
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void RangeInsertTest()
		{
			expectWrapper.ExecuteGenericCommand(string.Format("DELETE FROM {0} ", UsersMeta.TableName), null);

			var upperCountTestUsers = 100;
			var testUers = new List<Users>();

			var insGuid = Guid.NewGuid().ToString();

			for (var i = 0; i < upperCountTestUsers; i++)
			{
				testUers.Add(new Users { UserName = insGuid });
			}

			expectWrapper.InsertRange(testUers);

			var refSelect =
				expectWrapper.Database.Run(s => s.GetSkalar(string.Format("SELECT COUNT(*) FROM {0}", UsersMeta.TableName)));
			if (refSelect is long)
				refSelect = Convert.ChangeType(refSelect, typeof(int));

			Assert.AreEqual(testUers.Count, refSelect);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void Refresh()
		{
			InsertTest();
			var singleEntity = expectWrapper
				.Query()
				.Select<Users>()
				.Top(1)
				.ForResult<Users>()
				.Single();

			var id = singleEntity.UserID;
			Assert.IsNotNull(singleEntity);

			var preName = singleEntity.UserName;
			var postName = Guid.NewGuid().ToString();
			Assert.IsNotNull(preName);

			singleEntity.UserName = postName;
			expectWrapper.Update(singleEntity);
			singleEntity.UserName = null;

			singleEntity = expectWrapper.Refresh(singleEntity);
			var refEntity = expectWrapper.Select<Users>(id);

			Assert.IsNotNull(refEntity);
			Assert.AreEqual(id, refEntity.UserID);
			Assert.AreEqual(singleEntity.UserID, refEntity.UserID);
			Assert.AreEqual(singleEntity.UserName, refEntity.UserName);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void RefreshInplace()
		{
			InsertTest();
			var singleEntity = expectWrapper
				.Query()
				.Select<Base.TestModels.CheckWrapperBaseTests.Users>()
				.Top(1)
				.ForResult<Users>()
				.Single();
			var id = singleEntity.UserID;
			Assert.IsNotNull(singleEntity);

			var preName = singleEntity.UserName;
			var postName = Guid.NewGuid().ToString();
			Assert.IsNotNull(preName);

			singleEntity.UserName = postName;
			expectWrapper.Update(singleEntity);
			singleEntity.UserName = null;

			expectWrapper.RefreshKeepObject(singleEntity);
			var refEntity = expectWrapper.Select<Users>(id);

			Assert.IsNotNull(refEntity);
			Assert.AreEqual(id, refEntity.UserID);
			Assert.AreEqual(singleEntity.UserID, refEntity.UserID);
			Assert.AreEqual(singleEntity.UserName, refEntity.UserName);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SelectBase()
		{
			InsertTest();
			var refSelect = expectWrapper.Select<Users>();
			Assert.IsTrue(refSelect.Length > 0);

			var testInsertName = Guid.NewGuid().ToString();
			var testUser = expectWrapper.InsertWithSelect(new Users { UserName = testInsertName });
			Assert.IsNotNull(testUser);
			Assert.AreNotEqual(testUser.UserID, default(long));

			var selTestUser = expectWrapper.Select<Users_PK_IDFM_FUNCSELECTFACWITHPARAM>(testUser.UserID);
			Assert.AreEqual(selTestUser.UserName, testUser.UserName);
			Assert.AreEqual(selTestUser.UserId, testUser.UserID);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SelectWhereBase()
		{
			InsertTest();
			var refSelect = expectWrapper.SelectWhere<Users>("UserName IS NOT NULL");
			Assert.IsTrue(refSelect.Length > 0);

			var testInsertName = Guid.NewGuid().ToString();
			var testUser = expectWrapper.InsertWithSelect(new Users { UserName = testInsertName });
			Assert.IsNotNull(testUser);
			Assert.AreNotEqual(testUser.UserID, default(long));

			var selTestUser = expectWrapper.SelectWhere<Users>("User_ID = @id", new { id = testUser.UserID }).FirstOrDefault();
			Assert.AreEqual(selTestUser.UserName, testUser.UserName);
			Assert.AreEqual(selTestUser.UserID, testUser.UserID);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SelectModelsSelect()
		{
			RangeInsertTest();
			var firstAvaibleUser = expectWrapper.Query().Select<Base.TestModels.CheckWrapperBaseTests.Users>().Top(1).ForResult<Users>().First();

			var refSelect = expectWrapper.Select<Users_PK>(firstAvaibleUser.UserID);
			Assert.IsNotNull(refSelect);

			var userSelectAlternatingProperty = expectWrapper.Select<Users_PK_IDFM>(firstAvaibleUser.UserID);
			Assert.IsNotNull(userSelectAlternatingProperty);

			var userSelectStaticSel = expectWrapper.Select<Users_PK_IDFM_CLASSEL>(firstAvaibleUser.UserID);
			Assert.IsNotNull(userSelectStaticSel);
		}


		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SelectNative()
		{
			InsertTest();

			var refSelect = expectWrapper.SelectNative<Users>(UsersMeta.SelectStatement);
			Assert.IsTrue(refSelect.Any());

			var anyId = refSelect.FirstOrDefault().UserID;
			Assert.AreNotEqual(anyId, 0);

			refSelect =
				expectWrapper.SelectNative<Users>(UsersMeta.SelectStatement + " WHERE " + UsersMeta.PrimaryKeyName + " = @paramA",
					new QueryParameter("paramA", anyId));
			Assert.IsTrue(refSelect.Length > 0);

			refSelect =
				expectWrapper.SelectNative<Users>(
					UsersMeta.SelectStatement + " WHERE " + UsersMeta.PrimaryKeyName + " = @paramA", new { paramA = anyId });
			Assert.IsTrue(refSelect.Length > 0);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SelectPrimitivSelect()
		{
			InsertTest();
			var refSelect = expectWrapper.RunPrimetivSelect<long>(UsersMeta.SelectStatement);
			Assert.IsTrue(refSelect.Any());

			var anyId = refSelect.FirstOrDefault();
			Assert.AreNotEqual(anyId, 0);

			refSelect =
				expectWrapper.RunPrimetivSelect<long>(
					UsersMeta.SelectStatement + " WHERE " + UsersMeta.PrimaryKeyName + " = @paramA",
					new QueryParameter("paramA", anyId));
			Assert.IsTrue(refSelect.Length > 0);

			refSelect =
				expectWrapper.RunPrimetivSelect<long>(
					UsersMeta.SelectStatement + " WHERE " + UsersMeta.PrimaryKeyName + " = @paramA", new { paramA = anyId });
			Assert.IsTrue(refSelect.Length > 0);
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SelectPrimitivSelectNullHandling()
		{
			if (expectWrapper.DbAccessType != DbAccessType.MsSql)
				return;
			InsertTest();
			Assert.That(() =>
			{
				expectWrapper.RunPrimetivSelect<long>(
						UsersMeta.SelectStatement + " WHERE " + UsersMeta.PrimaryKeyName + " = @paramA",
						new QueryParameter("paramA", null));
			}, Throws.Exception);

			Assert.That(() =>
			{
				string n = null;
				expectWrapper.RunPrimetivSelect<long>(
					UsersMeta.SelectStatement + " WHERE " + UsersMeta.PrimaryKeyName + " = @paramA", new { paramA = n });
			}, Throws.Exception);
		}


		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void SyncCollectionTest()
		{
			RangeInsertTest();

			DbCollection<Users_Col> dbCollection = null;
			Assert.That(() => dbCollection = expectWrapper.CreateDbCollection<Users_Col>(), Throws.Nothing);
			Assert.That(dbCollection, Is.Not.Empty);
			Assert.That(dbCollection.Count, Is.EqualTo(100));

			Assert.That(() => dbCollection.Add(new Users_Col()), Throws.Nothing);
			Assert.That(dbCollection.Count, Is.EqualTo(101));

			Assert.That(() => dbCollection.SaveChanges(expectWrapper), Throws.Nothing);
			Assert.That(() => expectWrapper.Select<Users_Col>().Length, Is.EqualTo(101));

			Assert.That(() => dbCollection.Remove(dbCollection.First()), Throws.Nothing);
			Assert.That(dbCollection.Count, Is.EqualTo(100));

			Assert.That(() => dbCollection.SaveChanges(expectWrapper), Throws.Nothing);
			Assert.That(() => expectWrapper.Select<Users_Col>().Length, Is.EqualTo(100));

			var user25 = dbCollection[25];
			user25.UserName = Guid.NewGuid().ToString();

			Assert.That(() => dbCollection.GetEntryState(user25), Is.EqualTo(CollectionStates.Changed));
			Assert.That(() => dbCollection.SaveChanges(expectWrapper), Throws.Nothing);
			Assert.That(() => dbCollection.GetEntryState(user25), Is.EqualTo(CollectionStates.Unchanged));

			Assert.That(() => expectWrapper.Select<Users_Col>(user25.User_ID), Is.Not.Null.And
				.Property("User_ID").EqualTo(user25.User_ID)
				.And
				.Property("UserName").EqualTo(user25.UserName));
		}

		[Test]
		[Category("MsSQL")]
		[Category("SqLite")]
		public void Update()
		{
			InsertTest();
			var query = expectWrapper
				.Query()
				.Select<Users>()
				.Top(1);
			var singleEntity = query
				.ForResult<Users>()
				.Single();
			Assert.IsNotNull(singleEntity);

			var preName = singleEntity.UserName;
			var postName = Guid.NewGuid().ToString();
			Assert.IsNotNull(preName);

			singleEntity.UserName = postName;
			expectWrapper.Update(singleEntity);

			var refEntity = expectWrapper.Select<Users>(singleEntity.UserID);
			Assert.IsNotNull(refEntity);
			Assert.AreEqual(singleEntity.UserID, refEntity.UserID);
			Assert.AreEqual(singleEntity.UserName, refEntity.UserName);
		}
	}
}