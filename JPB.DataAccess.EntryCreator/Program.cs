﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JPB.DataAccess.EntryCreator.MsSql;

namespace JPB.DataAccess.EntryCreator
{
    public class Program
    {
        public Program()
        {

        }

        [STAThread]
        static void Main(string[] args)
        {
            var prog = new Program();
            if (!args.Any() || args.First().ToLower() == @"\?")
            {
                prog.RenderHelp();
            }
            var outputDir = @"D:\test";
            //string connectionString = "Data Source=S-SQL-DPE2;Initial Catalog=BatchRemoting;Integrated Security=False;User Id=BatchRemotingJobServer;Password=BatchServer;MultipleActiveResultSets=True";
            string connectionString = @"Data Source=(localdb)\Projects;Initial Catalog=TestDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False";

#if DEBUG
        
#else
            Console.WriteLine("Enter output dir");

            outputDir = Console.ReadLine();
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                Console.WriteLine("Invalid Directory ...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine(@"Enter Connection string or type \explore to search for a server [ToBeSupported]");
            if (Clipboard.ContainsText())
            {
                var maybeConnection = Clipboard.GetText(TextDataFormat.Text);
                var strings = maybeConnection.Split(';');
                var any = strings.Any(s => s.ToLower().Contains("data source=") || s.ToLower().Contains("initial catalog="));
                if (any)
                {
                    Console.WriteLine("Use clipboard content?");
                    var consoleKeyInfo = Console.ReadKey();
                    if (char.ToLower(consoleKeyInfo.KeyChar) == 'y')
                    {
                        connectionString = maybeConnection;
                    }
                    else
                    {
                        connectionString = string.Empty;
                    }
                }
                else
                {
                    connectionString = string.Empty;
                }
            }
            else
            {
                connectionString = string.Empty;
            }
            if (string.IsNullOrEmpty(connectionString))
                do
                {
                    connectionString = Console.ReadLine();
                    if (connectionString == @"\explore")
                    {
                        SqlDataSourceEnumerator instance = SqlDataSourceEnumerator.Instance;
                        Console.WriteLine("Search for data Sources in current network");
                        
                        var table = instance.GetDataSources();
                        Console.WriteLine("Row count {0}", table.Rows.Count);

                        foreach (var column in table.Columns.Cast<DataColumn>())
                        {
                            Console.Write(column.ColumnName + "|");
                        }

                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            var row = table.Rows[i];

                            Console.Write("o {0} |", i);

                            foreach (System.Data.DataColumn col in table.Columns)
                            {
                                Console.Write(" {0} = {1} |", col.ColumnName, row[col]);
                            }
                            Console.WriteLine("============================");
                        }
                        Console.WriteLine();
                    }
                } while (string.IsNullOrEmpty(connectionString) || connectionString.ToLower().Contains(@"\explore"));

#endif

            new MsSqlCreator().CreateEntrys(connectionString, outputDir, string.Empty);
        }

        private void RenderHelp()
        {
            Console.WriteLine("Creates cs Entrys based on a Database ( currently MsSQL only )");
            Console.WriteLine();
            Console.WriteLine("Usage: output [[/con]] ");
            Console.WriteLine();
            Console.WriteLine("\t output     A existing Directory where all cs classes are written to");
            Console.WriteLine("\t /con       Specifys the Connection property to a existing Database");
        }
    }
}
