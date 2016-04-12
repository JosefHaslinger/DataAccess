using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using JPB.DataAccess.DbInfoConfig;
using JPB.DataAccess.DbInfoConfig.DbInfo;
using JPB.DataAccess.EntityCreator.Core.Contracts;
using JPB.DataAccess.Helper;
using JPB.DataAccess.ModelsAnotations;
using Microsoft.CSharp;

namespace JPB.DataAccess.EntityCreator.Core.Compiler
{
	public abstract class ElementCompiler
	{
		static ElementCompiler()
		{
			Provider = new CSharpCodeProvider();
		}

		public ElementCompiler(string targetDir, string targetCsName)
		{
			_base = new CodeTypeDeclaration(targetCsName);
			TargetCsName = targetCsName;
			TargetDir = targetDir;
		}


		public const string GitURL = "https://github.com/JPVenson/DataAccess";
		public const string AttrbuteHeader = "JPB.DataAccess.EntityCreator.MsSql.MsSqlCreator";

		/// <summary>
		/// if set to true the output will be allways written
		/// </summary>
		public bool WriteAllways { get; set; }
		public string TargetDir { get; private set; }
		public string TableName { get; set; }
		public string TargetCsName { get; private set; }

		public IEnumerable<CodeTypeMember> Members
		{
			get { return _base.Members.Cast<CodeTypeMember>().Where(s => s != null); }
		}

		public CodeTypeMemberCollection MembersFromBase
		{
			get { return _base.Members; }
		}

		public string Name { get { return _base.Name; } }

		public bool CompileHeader { get; set; }

		public bool GenerateConfigMethod { get; set; }

		protected CodeTypeDeclaration _base;

		static readonly CodeDomProvider Provider;

		public string Namespace { get; set; }

		public void Add(CodeTypeMember property)
		{
			_base.Members.Add(property);
		}

		public abstract void PreCompile();

		private static string debug(CodeCompileUnit cp)
		{
			var writer = new StringWriter();
			writer.NewLine = Environment.NewLine;

			new CSharpCodeProvider().GenerateCodeFromCompileUnit(cp, writer, new CodeGeneratorOptions {
				BlankLinesBetweenMembers = false,
				VerbatimOrder = true,
				ElseOnClosing = true
			});

			writer.Flush();
			return writer.ToString();
		}

		public List<DbPropertyInfoCache> ColumninfosToInfoCache(IEnumerable<IColumInfoModel> columnInfos)
		{
			var dic = new List<DbPropertyInfoCache>();

			foreach (var item in columnInfos)
			{
				var dbInfoCache = new DbPropertyInfoCache();
				dic.Add(dbInfoCache);
				dbInfoCache.PropertyName = item.GetPropertyName();
				dbInfoCache.PropertyType = item.ColumnInfo.TargetType;
				if (item.NewColumnName != item.ColumnInfo.ColumnName)
				{
					dbInfoCache.Attributes.Add(new DbAttributeInfoCache(new ForModelAttribute(item.ColumnInfo.ColumnName)));
				}
				if (item.IsRowVersion)
				{
					dbInfoCache.Attributes.Add(new DbAttributeInfoCache(new RowVersionAttribute()));
				}
				if (item.InsertIgnore)
				{
					dbInfoCache.Attributes.Add(new DbAttributeInfoCache(new InsertIgnoreAttribute()));
				}
				if (item.PrimaryKey)
				{
					dbInfoCache.Attributes.Add(new DbAttributeInfoCache(new PrimaryKeyAttribute()));
				}
				if (item.ForgeinKeyDeclarations != null)
				{
					dbInfoCache.Attributes.Add(new DbAttributeInfoCache(new ForeignKeyDeclarationAttribute(item.ForgeinKeyDeclarations.TargetColumn, item.ForgeinKeyDeclarations.TableName)));
				}
				if (item.EnumDeclaration != null)
				{
					dbInfoCache.Attributes.Add(new DbAttributeInfoCache(new ValueConverterAttribute(typeof(EnumMemberConverter))));
				}

				dbInfoCache.Refresh();
			}
			return dic;
		}

		public virtual void Compile(IEnumerable<IColumInfoModel> columnInfos, Stream to = null)
		{
			if (to != null)
			{
				if (!to.CanSeek)
					throw new InvalidOperationException("The stream must be seekable");
				if (!to.CanWrite)
					throw new InvalidOperationException("The stream must be writeable");
			}

			this.PreCompile();
			if (string.IsNullOrEmpty(TableName))
			{
				TableName = TargetCsName;
			}

			var comments = (new[]
			{
				new CodeCommentStatement("Created by " + Environment.UserDomainName + @"\" + Environment.UserName),
				new CodeCommentStatement("Created on " + DateTime.Now.ToString("yyyy MMMM dd"))
			}).ToArray();

			//Create DOM class
			_base.Name = TargetCsName;

			if (CompileHeader)
				_base.Comments.AddRange(comments);

			//Write static members
			_base.TypeAttributes = TypeAttributes.Sealed | TypeAttributes.Public;
			_base.IsPartial = true;

			CodeNamespace importNameSpace;

			if (string.IsNullOrEmpty(Namespace))
			{
				importNameSpace = new CodeNamespace("JPB.DataAccess.EntryCreator.AutoGeneratedEntrys");
			}
			else
			{
				importNameSpace = new CodeNamespace(Namespace);
			}

			//Add Code Generated Attribute
			var generatedCodeAttribute = new GeneratedCodeAttribute(AttrbuteHeader, "1.0.0.8");
			var codeAttrDecl = new CodeAttributeDeclaration(generatedCodeAttribute.GetType().Name,
				new CodeAttributeArgument(
					new CodePrimitiveExpression(generatedCodeAttribute.Tool)),
				new CodeAttributeArgument(
					new CodePrimitiveExpression(generatedCodeAttribute.Version)));

			_base.CustomAttributes.Add(codeAttrDecl);
			//Add members

			if (GenerateConfigMethod)
			{
				GenerateConfigMehtod(columnInfos, importNameSpace);
			}
			else
			{
				if (!string.IsNullOrEmpty(TableName) && TableName != TargetCsName)
				{
					var forModel = new ForModelAttribute(TableName);
					var codeAttributeDeclaration = new CodeAttributeDeclaration(forModel.GetType().Name,
						new CodeAttributeArgument(new CodePrimitiveExpression(forModel.AlternatingName)));
					_base.CustomAttributes.Add(codeAttributeDeclaration);
				}
			}

			var compileUnit = new CodeCompileUnit();

			using (var memStream = new MemoryStream())
			{
				using (var writer = new StreamWriter(memStream, Encoding.UTF8, 128, true))
				{
					writer.NewLine = Environment.NewLine;

					var cp = new CompilerParameters();
					cp.ReferencedAssemblies.Add("System.dll");
					cp.ReferencedAssemblies.Add("System.Core.dll");
					cp.ReferencedAssemblies.Add("System.Data.dll");
					cp.ReferencedAssemblies.Add("System.Xml.dll");
					cp.ReferencedAssemblies.Add("System.Xml.Linq.dll");
					cp.ReferencedAssemblies.Add("JPB.DataAccess.dll");
					
					importNameSpace.Imports.Add(new CodeNamespaceImport("System"));
					importNameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
					importNameSpace.Imports.Add(new CodeNamespaceImport("System.CodeDom.Compiler"));
					importNameSpace.Imports.Add(new CodeNamespaceImport("System.Linq"));
					importNameSpace.Imports.Add(new CodeNamespaceImport("System.Data"));
					importNameSpace.Imports.Add(new CodeNamespaceImport(typeof(ForModelAttribute).Namespace));
					importNameSpace.Types.Add(_base);
					compileUnit.Namespaces.Add(importNameSpace);

					Provider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions() {
						BlankLinesBetweenMembers = false,
						BracingStyle = "C",
						IndentString = "	",
						VerbatimOrder = true,
						ElseOnClosing = true
					});

					Console.WriteLine("Generated class" + _base.Name);
					writer.Flush();
				}

				Console.WriteLine("Compute changes");
				//check if hascodes are diverent
				var hasher = MD5.Create();
				var neuHash = hasher.ComputeHash(memStream.ToArray());
				var targetFileName = Path.Combine(TargetDir, _base.Name + ".cs");
				if (to == null)
				{
					using (var fileStream = new FileStream(targetFileName, FileMode.OpenOrCreate))
					{
						var exisitingHash = hasher.ComputeHash(fileStream);
						if (!exisitingHash.SequenceEqual(neuHash))
						{
							Console.WriteLine("Class changed. Old file will be kept and new contnt will be written");
							fileStream.SetLength(0);
							fileStream.Flush();
							fileStream.Seek(0, SeekOrigin.Begin);
							memStream.WriteTo(fileStream);
							memStream.Flush();
							fileStream.Flush();
						}
					}
					
				}
				else
				{
					var exisitingHash = hasher.ComputeHash(to);
					if (WriteAllways || !exisitingHash.SequenceEqual(neuHash))
					{
						Console.WriteLine("Class changed. Old file will be kept and new contnt will be written");
						to.SetLength(0);
						to.Flush();
						to.Seek(0, SeekOrigin.Begin);
						memStream.WriteTo(to);
						memStream.Flush();
						to.Flush();
					}
				}
			}
		}

		private void GenerateConfigMehtod(IEnumerable<IColumInfoModel> columnInfos, CodeNamespace importNameSpace)
		{
			const string configArgumentName = "config";

			var configArgument = string.Format("ConfigurationResolver<{0}>", TargetCsName);

			importNameSpace.Imports.Add(new CodeNamespaceImport(typeof (ConfigurationResolver<>).Namespace));

			string[] eventNames = new string[]
			{
				"BeforeConfig()", "AfterConfig()", "BeforeConfig(" + configArgument + " config)",
				"AfterConfig(" + configArgument + " config)"
			};

			foreach (string eventName in eventNames)
			{
				CodeMemberField eventHook = new CodeMemberField(); //do it as a FIELD instead of a METHOD
				eventHook.Name = eventName;
				eventHook.Attributes = MemberAttributes.Static;
				eventHook.Type = new CodeTypeReference("partial void");
				_base.Members.Add(eventHook);
			}

			var configMethod = new CodeMemberMethod();
			configMethod.Attributes = MemberAttributes.Static | MemberAttributes.Public;

			configMethod.CustomAttributes.Add(new CodeAttributeDeclaration(typeof (ConfigMehtodAttribute).Name));
			configMethod.Name = "Config" + TargetCsName;
			configMethod.Parameters.Add(new CodeParameterDeclarationExpression(configArgument, configArgumentName));
			var configRef = new CodeVariableReferenceExpression(configArgumentName);

			configMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(TargetCsName),
				"BeforeConfig"));
			configMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(TargetCsName),
				"BeforeConfig", configRef));

			if (!string.IsNullOrEmpty(TableName) && TableName != TargetCsName)
			{
				var setNewTablenameOnConfig = new CodeMethodInvokeExpression(configRef,
					"SetClassAttribute",
					new CodeObjectCreateExpression(typeof (ForModelAttribute).Name,
						new CodePrimitiveExpression(TableName)));
				configMethod.Statements.Add(setNewTablenameOnConfig);
			}

			foreach (var member in this.MembersFromBase.Cast<CodeTypeMember>().ToArray())
			{
				if (member is CodeMemberProperty || (member is CodeMemberMethod && !(member is CodeConstructor)))
				{
					var property = member as CodeTypeMember;
					if (property.CustomAttributes != null)
						foreach (CodeAttributeDeclaration attribute in property.CustomAttributes)
						{
							var dbAttribute = new CodeObjectCreateExpression(attribute.Name);
							foreach (CodeAttributeArgument attributeArgument in attribute.Arguments)
							{
								dbAttribute.Parameters.Add(attributeArgument.Value);
							}
							var methodName = (member is CodeMemberProperty) ? "SetPropertyAttribute" : "SetMethodAttribute";
							var propDelegate = new CodeSnippetExpression("s => s." + property.Name);
							var configPropCall = new CodeMethodInvokeExpression(configRef, methodName, propDelegate, dbAttribute);

							configMethod.Statements.Add(configPropCall);
						}

					property.CustomAttributes.Clear();
				}

				if (member is CodeConstructor)
				{
					var ctor = member as CodeConstructor;
					if (
						ctor.CustomAttributes.Cast<CodeAttributeDeclaration>()
							.Any(f => f.Name == typeof (ObjectFactoryMethodAttribute).Name))
					{
						var dbInfos = ColumninfosToInfoCache(columnInfos);

						var codeFactory = FactoryHelper.GenerateTypeConstructor(true);
						var super = new CodeVariableReferenceExpression("super");

						var pocoVariable = new CodeVariableDeclarationStatement(TargetCsName, "super");
						codeFactory.Statements.Add(pocoVariable);

						var codeAssignment = new CodeAssignStatement(super, new CodeObjectCreateExpression(TargetCsName));
						codeFactory.Statements.Add(codeAssignment);
						FactoryHelper.GenerateBody(dbInfos.ToDictionary(s => s.DbName),
							new FactoryHelperSettings(),
							importNameSpace,
							codeFactory,
							super);

						codeFactory.ReturnType = new CodeTypeReference(TargetCsName);
						codeFactory.Statements.Add(new CodeMethodReturnStatement(super));
						_base.Members.Add(codeFactory);
						_base.Members.Remove(member);
						var factorySet = new CodeMethodInvokeExpression(configRef,
							"SetFactory",
							new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(TargetCsName), codeFactory.Name),
							new CodePrimitiveExpression(true));
						configMethod.Statements.Add(factorySet);
					}
				}
			}
			configMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(TargetCsName), "AfterConfig"));
			configMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(TargetCsName), "AfterConfig",
				configRef));

			_base.Members.Add(configMethod);
		}
	}
}