﻿#region

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using AutoMapper;
using JPB.DataAccess.Contacts;
using JPB.DataAccess.DbInfoConfig;
using JPB.DataAccess.EntityCreator.Core;
using JPB.DataAccess.EntityCreator.Core.Contracts;
using JPB.DataAccess.EntityCreator.Core.Models;
using JPB.DataAccess.EntityCreator.Core.Poco;
using JPB.DataAccess.EntityCreator.UI.MsSQL.Services;
using JPB.DataAccess.EntityCreator.UI.Shared.Model;
using JPB.DataAccess.Manager;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using Microsoft.Data.ConnectionUI;
using Microsoft.WindowsAPICodePack.Dialogs;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

#endregion

namespace JPB.DataAccess.EntityCreator.UI.MsSQL.ViewModel
{
	public class SqlEntityCreatorViewModel : AsyncViewModelBase, IMsSqlCreator
	{
		private bool _connected;
		private string _connectionString;
		private ThreadSaveObservableCollection<Dictionary<int, string>> _enums;
		private bool _generateCompilerHeader;
		private bool _generateConfigMethod;
		private bool _generateConstructor;
		private bool _generateForgeinKeyDeclarations;
		private bool _isEnumeratingDatabase;
		private string _namespace;
		private ITableInfoModel _selectedTable;
		private string _status;
		private ThreadSaveObservableCollection<IStoredPrcInfoModel> _storedProcs;
		private ThreadSaveObservableCollection<TableInfoViewModel> _tables;
		private ThreadSaveObservableCollection<TableInfoViewModel> _views;
		private bool _wrapNullables;

		public SqlEntityCreatorViewModel()
		{
			AdjustNamesCommand = new DelegateCommand(AdjustNamesExecute, CanAdjustNamesExecute);
			CompileCommand = new DelegateCommand(CompileExecute, CanCompileExecute);
			ConnectToDatabaseCommand = new DelegateCommand(ConnectToDatabaseExecute, CanConnectToDatabaseExecute);
			SaveConfigCommand = new DelegateCommand(SaveConfigExecute, CanSaveConfigExecute);
			LoadConfigCommand = new DelegateCommand(LoadConfigExecute, CanLoadConfigExecute);
			OpenInfoWindowCommand = new DelegateCommand(OpenInfoWindowExecute);
			DeleteSelectedTableCommand = new DelegateCommand(DeleteSelectedTableExecute, CanDeleteSelectedTableExecute);
			AddTableCommand = new DelegateCommand(AddTableExecute, CanAddTableExecute);

			Tables = new ThreadSaveObservableCollection<TableInfoViewModel>();
			Views = new ThreadSaveObservableCollection<TableInfoViewModel>();
			StoredProcs = new ThreadSaveObservableCollection<IStoredPrcInfoModel>();
			Enums = new ThreadSaveObservableCollection<Dictionary<int, string>>();
			SharedInterfaces = new ThreadSaveObservableCollection<ISharedInterface>();
			SharedMethods.Logger = new DelegateLogger(message => Status = message);
		}

		public DelegateCommand OpenInfoWindowCommand { get; set; }
		public DelegateCommand LoadConfigCommand { get; private set; }
		public DelegateCommand SaveConfigCommand { get; private set; }
		private bool _splitByType;

		public bool SplitByType
		{
			get { return _splitByType; }
			set
			{
				SendPropertyChanging(() => SplitByType);
				_splitByType = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => SplitByType);
			}
		}

		public ITableInfoModel SelectedTable
		{
			get { return _selectedTable; }
			set
			{
				SendPropertyChanging(() => SelectedTable);
				_selectedTable = value;
				SendPropertyChanged(() => SelectedTable);
			}
		}

		public ThreadSaveObservableCollection<TableInfoViewModel> Tables
		{
			get { return _tables; }
			set
			{
				SendPropertyChanging(() => Tables);
				_tables = value;
				SendPropertyChanged(() => Tables);
			}
		}

		public ThreadSaveObservableCollection<Dictionary<int, string>> Enums
		{
			get { return _enums; }
			set
			{
				SendPropertyChanging(() => Enums);
				_enums = value;
				SendPropertyChanged(() => Enums);
			}
		}

		public ThreadSaveObservableCollection<TableInfoViewModel> Views
		{
			get { return _views; }
			set
			{
				SendPropertyChanging(() => Views);
				_views = value;
				SendPropertyChanged(() => Views);
			}
		}

		public ThreadSaveObservableCollection<IStoredPrcInfoModel> StoredProcs
		{
			get { return _storedProcs; }
			set
			{
				SendPropertyChanging(() => StoredProcs);
				_storedProcs = value;
				SendPropertyChanged(() => StoredProcs);
			}
		}

		public string ConnectionString
		{
			get { return _connectionString; }
			set
			{
				SendPropertyChanging(() => ConnectionString);
				_connectionString = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => ConnectionString);
			}
		}

		public bool NotConnected
		{
			get { return !Connected; }
		}

		public bool Connected
		{
			get { return _connected; }
			set
			{
				SendPropertyChanging(() => NotConnected);
				SendPropertyChanging(() => Connected);
				_connected = value;
				SendPropertyChanged(() => NotConnected);
				SendPropertyChanged(() => Connected);
			}
		}

		public DelegateCommand ConnectToDatabaseCommand { get; private set; }

		public bool IsEnumeratingDatabase
		{
			get { return _isEnumeratingDatabase; }
			set
			{
				if (value == false)
				{
					Status =
							string.Format("Found {0} Tables, {1} Views, {2} Procedures ... select a Table to see Options or start an Action",
							Tables.Count, Views.Count, StoredProcs.Count);
				}

				SendPropertyChanging(() => IsEnumeratingDatabase);
				_isEnumeratingDatabase = value;
				SendPropertyChanged(() => IsEnumeratingDatabase);
			}
		}

		public DbAccessLayer Manager { get; set; }

		public string Status
		{
			get { return _status; }
			set
			{
				SendPropertyChanging(() => Status);
				_status = value;
				SendPropertyChanged(() => Status);
			}
		}

		public DelegateCommand AddTableCommand { get; private set; }

		public DelegateCommand DeleteSelectedTableCommand { get; private set; }

		public DelegateCommand CompileCommand { get; private set; }

		public DelegateCommand AdjustNamesCommand { get; private set; }

		IEnumerable<ITableInfoModel> IMsSqlCreator.Tables
		{
			get { return Tables; }
			set { }
		}

		IEnumerable<ITableInfoModel> IMsSqlCreator.Views
		{
			get { return Views; }
			set { }
		}

		IEnumerable<Dictionary<int, string>> IMsSqlCreator.Enums { get; }

		public bool WrapNullables
		{
			get { return _wrapNullables; }
			set
			{
				SendPropertyChanging(() => WrapNullables);
				_wrapNullables = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => WrapNullables);
			}
		}

		IEnumerable<IStoredPrcInfoModel> IMsSqlCreator.StoredProcs
		{
			get { return StoredProcs; }
		}

		public string TargetDir { get; set; }

		public bool GenerateConstructor
		{
			get { return _generateConstructor; }
			set
			{
				SendPropertyChanging(() => GenerateConstructor);
				_generateConstructor = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => GenerateConstructor);
			}
		}

		public string SqlVersion { get; set; }

		public string Namespace
		{
			get { return _namespace; }
			set
			{
				SendPropertyChanging(() => Namespace);
				_namespace = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => Namespace);
			}
		}

		public bool GenerateForgeinKeyDeclarations
		{
			get { return _generateForgeinKeyDeclarations; }
			set
			{
				SendPropertyChanging(() => GenerateForgeinKeyDeclarations);
				_generateForgeinKeyDeclarations = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => GenerateForgeinKeyDeclarations);
			}
		}

		public bool GenerateConfigMethod
		{
			get { return _generateConfigMethod; }
			set
			{
				SendPropertyChanging(() => GenerateConfigMethod);
				_generateConfigMethod = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => GenerateConfigMethod);
			}
		}

		public bool GenerateCompilerHeader
		{
			get { return _generateCompilerHeader; }
			set
			{
				SendPropertyChanging(() => GenerateCompilerHeader);
				_generateCompilerHeader = value;
				MementoService.Instance.SetOption(new SetPropertyAction(value));
				SendPropertyChanged(() => GenerateCompilerHeader);
			}
		}

		public IEnumerable<ISharedInterface> SharedInterfaces { get; set; }

		public void CreateEntrys(string connection, string outputPath, string database)
		{
			var entrysAsync = CreateEntrysAsync(connection, outputPath, database);
		}

		public void Compile()
		{
			var path = "";
			DialogResult dirResult;
			dirResult = DialogResult.Retry;

			if (CommonFileDialog.IsPlatformSupported)
			{
				var dialog = new CommonOpenFileDialog();
				dialog.IsFolderPicker = true;
				ThreadSaveAction(
				() => { dirResult = dialog.ShowDialog() == CommonFileDialogResult.Ok ? DialogResult.OK : DialogResult.Abort; });
				if (dirResult == DialogResult.OK)
				{
					path = dialog.FileName;
				}
			}
			else
			{
				var dir = new FolderBrowserDialog();
				ThreadSaveAction(() => { dirResult = dir.ShowDialog(); });
				if (dirResult == DialogResult.OK)
				{
					path = dir.SelectedPath;
				}
			}

			if (dirResult == DialogResult.OK)
			{
				TargetDir = path;
				foreach (var tableInfoModel in Tables)
				{
					Status = string.Format("Compiling Table '{0}'", tableInfoModel.GetClassName());
					SharedMethods.CompileTable(tableInfoModel, this);
				}

				foreach (var tableInfoModel in Views)
				{
					Status = string.Format("Compiling View '{0}'", tableInfoModel.GetClassName());
					SharedMethods.CompileTable(tableInfoModel, this);
				}
			}
		}

		public async Task CreateEntrysAsync(string connection, string outputPath, string database)
		{
			Status = "Try to connect";
			//Data Source=(LocalDb)\ProjectsV12;Integrated Security=True;Database=TestDB;
			IsEnumeratingDatabase = true;
			TargetDir = outputPath;
			Manager = new DbAccessLayer(DbAccessType.MsSql, connection);
			Manager.Async = false;
			Manager.ThreadSave = true;
			DbConfig.EnableGlobalThreadSafety = true;
			try
			{
				Connected = Manager.CheckDatabase();
			}
			catch (Exception)
			{
				IsEnumeratingDatabase = false;
				Connected = false;
			}

			if (!Connected)
			{
				IsEnumeratingDatabase = false;
				Status = "Database not accessible. Maybe wrong Connection or no Selected Database?";
				return;
			}

			var databaseName = string.IsNullOrEmpty(Manager.Database.DatabaseName) ? database : Manager.Database.DatabaseName;
			if (string.IsNullOrEmpty(databaseName))
			{
				IsEnumeratingDatabase = false;
				Status = "Database not exists. Maybe wrong Connection or no Selected Database?";
				Connected = false;
				return;
			}

			Status = "Connection OK ... Reading Server Version ...";

			SqlVersion = Manager.RunPrimetivSelect<string>("SELECT SERVERPROPERTY('productversion')").FirstOrDefault();
			Status = "Reading Tables";

			var counter = 2;
			var createTables = SimpleWork(() =>
			{
				var tables = new DbAccessLayer(DbAccessType.MsSql, connection).Select<TableInformations>()
				                                                              .Select(
				                                                              s =>
						                                                              new TableInfoModel(s, databaseName,
						                                                              new DbAccessLayer(DbAccessType.MsSql, connection)))
				                                                              .Select(s => new TableInfoViewModel(s, this))
				                                                              .ToList();
				foreach (var source in tables)
				{
					if (Tables.All(f => f.Info.TableName != source.Info.TableName))
					{
						Tables.Add(source);
					}
				}
			});
			var createViews = SimpleWork(() =>
			{
				var views =
						new DbAccessLayer(DbAccessType.MsSql, connection)
								.Select<ViewInformation>()
								.Select(s => new TableInfoModel(s, databaseName, new DbAccessLayer(DbAccessType.MsSql, connection)))
								.Select(s => new TableInfoViewModel(s, this))
								.ToList();

				foreach (var source in views)
				{
					if (Views.All(f => f.Info.TableName != source.Info.TableName))
					{
						Views.Add(source);
					}
				}
			});

			await createTables;
			await createViews;
			SelectedTable = Tables.FirstOrDefault();

			IsEnumeratingDatabase = false;
			Status = "Done";
		}

		public void OpenInfoWindowExecute(object sender)
		{
			new InfoWindow().ShowDialog();
		}

		private void LoadConfigExecute(object sender)
		{
			var fileDialog = new OpenFileDialog();
			fileDialog.Multiselect = false;
			fileDialog.CheckFileExists = true;
			fileDialog.DefaultExt = "*.msConfigStore";
			fileDialog.Filter = "ConfigFile (*.msConfigStore)|*.msConfigStore";
			var result = fileDialog.ShowDialog();
			if (result.HasValue && result.Value && File.Exists(fileDialog.FileName))
			{
				Tables.Clear();
				Views.Clear();
				StoredProcs.Clear();
				SelectedTable = null;

				var binFormatter = new BinaryFormatter();
				ConfigStore options;
				try
				{
					using (var fs = fileDialog.OpenFile())
					{
						options = (ConfigStore) binFormatter.Deserialize(fs);
					}
				}
				catch (Exception)
				{
					Status = "File is an in invalid format";
					return;
				}

				var version = typeof(SharedMethods).Assembly.GetName().Version;
				if (new Version(options.Version) != version)
				{
					var messageBoxResult = MessageBox.Show(Application.Current.MainWindow,
					"Warning Version missmatch",
					string.Format("The current Entity Creator version ({0}) is not equals the version ({1}) you have provided.",
					version, options.Version),
					MessageBoxButton.OKCancel);

					if (messageBoxResult == MessageBoxResult.Cancel)
					{
						return;
					}
				}

				if (options.SourceConnectionString != null)
				{
					ConnectionString = options.SourceConnectionString;
					CreateEntrysAsync(ConnectionString, "", string.Empty).ContinueWith(task =>
					{
						foreach (var optionsAction in options.Actions)
						{
							optionsAction.Replay(this);
						}
					});
				}
			}
		}

		private bool CanLoadConfigExecute(object sender)
		{
			return IsNotWorking;
		}

		private void SaveConfigExecute(object sender)
		{
			var fileDialog = new SaveFileDialog();
			fileDialog.DefaultExt = ".msConfigStore";
			fileDialog.Filter = "ConfigFile (*.msConfigStore)|*.msConfigStore";
			fileDialog.CheckFileExists = false;
			var fileResult = fileDialog.ShowDialog();
			if (fileResult == DialogResult.OK)
			{
				var options = new ConfigStore();
				options.Actions = MementoService.Instance.Actions;

				var version = typeof(SharedMethods).Assembly.GetName().Version;

				options.Version = version.ToString();

				if (ConnectionString != null)
				{
					options.SourceConnectionString = ConnectionString;
				}

				if (File.Exists(fileDialog.FileName))
				{
					File.Delete(fileDialog.FileName);
				}

				using (var fs = fileDialog.OpenFile())
				{
					new BinaryFormatter().Serialize(fs, options);
				}
			}
		}

		private bool CanSaveConfigExecute(object sender)
		{
			return StoredProcs.Any() || Views.Any() || Tables.Any();
		}

		private void ConnectToDatabaseExecute(object sender)
		{
			var dcd = new DataConnectionDialog();
			dcd.DataSources.Add(DataSource.SqlDataSource);

			if (DataConnectionDialog.Show(dcd) == DialogResult.OK)
			{
				if (string.IsNullOrEmpty(dcd.ConnectionString))
				{
					return;
				}

				var sqlConnectionString = new SqlConnectionStringBuilder(dcd.ConnectionString);
				if (string.IsNullOrEmpty(sqlConnectionString.DataSource))
				{
					Status = "Please provide a DataSource";
					return;
				}

				ConnectionString = dcd.ConnectionString;
				var entrysAsync = CreateEntrysAsync(ConnectionString, "C:\\", string.Empty);
			}
		}

		private bool CanConnectToDatabaseExecute(object sender)
		{
			return CheckCanExecuteCondition() && !IsEnumeratingDatabase;
		}

		private void AddTableExecute(object sender)
		{
			Tables.Add(new TableInfoViewModel(new TableInfoModel
			{
					Info = new TableInformations
					{
							TableName = "New Table"
					}
			}, this));
			MementoService.Instance.SetOption(new CreateTableAction("New Table"));
		}

		private bool CanAddTableExecute(object sender)
		{
			return true;
		}

		private void DeleteSelectedTableExecute(object sender)
		{
			Tables.Remove(SelectedTable);
			MementoService.Instance.SetOption(new CreateTableAction(SelectedTable.Info.TableName));
			SelectedTable = null;
		}

		private bool CanDeleteSelectedTableExecute(object sender)
		{
			return SelectedTable != null;
		}

		private void CompileExecute(object sender)
		{
			SimpleWork(Compile);
		}

		private bool CanCompileExecute(object sender)
		{
			return CheckCanExecuteCondition();
		}

		private void AdjustNamesExecute(object sender)
		{
			SimpleWork(() => { SharedMethods.AutoAlignNames(Tables); });
			SimpleWork(() => { SharedMethods.AutoAlignNames(Views); });
		}

		private bool CanAdjustNamesExecute(object sender)
		{
			return CheckCanExecuteCondition();
		}
	}

	public class DelegateLogger : ILogger
	{
		private readonly Action<string> _resolve;

		public DelegateLogger(Action<string> resolve)
		{
			_resolve = resolve;
		}

		public void Write(string content, params object[] arguments)
		{
			_resolve(string.Format(content, arguments));
		}

		public void WriteLine(string content = null, params object[] arguments)
		{
			if (content == null)
			{
				_resolve(Environment.NewLine);
				return;
			}

			_resolve(string.Format(content, arguments));
			_resolve(Environment.NewLine);
		}
	}
}