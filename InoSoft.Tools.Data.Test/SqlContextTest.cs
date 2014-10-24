using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace InoSoft.Tools.Data.Test
{
    [TestFixture]
    public class SqlContextTest
    {
        #region Configuration

        private const string DbName = "InoSoft.Tools.Data.Test";
        private const string DbServer = ".";

        private static SqlContext<IProceduresProxy> CreateSqlContext()
        {
            return new SqlContext<IProceduresProxy>(GetConnectionString(DbName));
        }

        private static string GetConnectionString(string catalog)
        {
            return String.Format("data source={0};initial catalog={1};integrated security=true", DbServer, catalog);
        }

        #endregion Configuration

        #region Set up / tear down

        [TestFixtureSetUp]
        public void SetUp()
        {
            string[] commands = ResourceHelper.ReadText("Database.sql")
                .Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
            using (var context = new SqlContext(GetConnectionString(DbName), true))
            {
                foreach (string command in commands)
                {
                    context.Execute(command);
                }
            }
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            SqlConnection.ClearAllPools();
            using (var master = new SqlContext(GetConnectionString("master")))
            {
                DropDatabase(master, DbName);
                DropDatabase(master, "nonexistent");
            }
        }

        private static void DropDatabase(ISqlContext masterContext, string dbName)
        {
            masterContext.Execute(String.Format("IF db_id('{0}') IS NOT NULL DROP DATABASE [{0}]", dbName));
        }

        private static void InsertHuman(ISqlContext context, Human human)
        {
            context.Execute("INSERT INTO Human VALUES(@id, @firstName, @lastName)",
                new SqlParameter("id", human.Id.HasValue ? (object)human.Id.Value : DBNull.Value),
                new SqlParameter("firstName", human.FirstName),
                new SqlParameter("lastName", human.LastName));
        }

        #endregion Set up / tear down

        [Test]
        public void ConnectionOptions()
        {
            using (var context = CreateSqlContext())
            {
                bool arithabort = context.Execute<bool>("SELECT CAST(SESSIONPROPERTY('ARITHABORT') AS bit)").Single();
                Assert.AreEqual(true, arithabort);
            }
        }

        [Test]
        public void CreateDatabase()
        {
            using (var nonexistent = new SqlContext(GetConnectionString("nonexistent"), true))
            {
                int hundred = nonexistent.Execute<int>("SELECT 100").Single();
                Assert.AreEqual(100, hundred);
            }
        }

        [Test]
        public void Enum()
        {
            using (var context = CreateSqlContext())
            {
                Human[] testHumans =
                {
                    new Human { Id = null, FirstName = "Josef", LastName = "Kobzon" },
                    new Human { Id = 2, FirstName = "Sofia", LastName = "Rotaru" },
                    new Human { Id = 3, FirstName = "Larisa", LastName = "Dolina" },
                };
                HumanId?[] testHumanIds =
                {
                    null,
                    HumanId.Rotaru,
                    HumanId.Dolina
                };
                context.Execute("TRUNCATE TABLE Human");
                foreach (var item in testHumans)
                {
                    InsertHuman(context, item);
                }

                for (int i = 0; i < 3; i++)
                {
                    var resultHuman = context.Procedures.GetHumanById(testHumanIds[i]);
                    Assert.IsTrue(testHumans[i].MemberwiseEquals(resultHuman));
                    if (testHumanIds[i].HasValue)
                    {
                        resultHuman = context.Procedures.GetHumanById(testHumanIds[i].Value);
                        Assert.IsTrue(testHumans[i].MemberwiseEquals(resultHuman));
                    }
                }
            }
        }

        [Test]
        public void GetHumanById()
        {
            using (var context = CreateSqlContext())
            {
                context.Execute("DELETE Human WHERE Id = 100 OR Id = 101");
                Human testHuman = new Human { Id = 100, FirstName = "Josef", LastName = "Kobzon" };
                InsertHuman(context, testHuman);

                Human resultHuman = context.Procedures.GetHumanById(100);
                Assert.IsTrue(testHuman.MemberwiseEquals(resultHuman));

                resultHuman = context.Procedures.GetHumanById(101);
                Assert.IsNull(resultHuman);
            }
        }

        [Test]
        public void GetHumans()
        {
            using (var context = CreateSqlContext())
            {
                Human[] testHumans =
                {
                    new Human { Id = 1, FirstName = "Josef", LastName = "Kobzon" },
                    new Human { Id = 2, FirstName = "Sofia", LastName = "Rotaru" },
                    new Human { Id = 3, FirstName = "Larisa", LastName = "Dolina" }
                };

                context.Execute("TRUNCATE TABLE Human");
                foreach (var item in testHumans)
                {
                    InsertHuman(context, item);
                }

                var resultHumans = context.Procedures.GetHumans();
                Assert.IsTrue(testHumans.ElementwiseEquals(resultHumans));
            }
        }

        [Test]
        public void Nulls()
        {
            using (var context = CreateSqlContext())
            {
                Human[] testHumans =
                {
                    new Human { Id = null, FirstName = "Josef", LastName = "Kobzon" },
                    new Human { Id = 2, FirstName = null, LastName = "Rotaru" },
                    new Human { Id = 3, FirstName = "Larisa", LastName = null }
                };
                context.Execute("TRUNCATE TABLE Human");
                foreach (var item in testHumans)
                {
                    context.Procedures.AddHuman(item.Id, item.FirstName, item.LastName);
                }

                var resultHumans = context.Execute<Human>("SELECT * FROM Human");

                Assert.IsTrue(testHumans.ElementwiseEquals(resultHumans));
            }
        }

        [Test]
        public void ProcessText()
        {
            using (var context = CreateSqlContext())
            {
                var sb = new StringBuilder();
                for (int i = 0; i < 100; i++)
                {
                    sb.Append("abcd");
                }
                var testText = sb.ToString();
                var resultText = context.Procedures.ProcessText(testText);
                Assert.AreEqual(testText, resultText);
            }
        }

        [Test]
        public void StringOutputs()
        {
            using (var context = CreateSqlContext())
            {
                Human testHuman = new Human { Id = 100, FirstName = "Josef", LastName = "Kobzon" };
                context.Execute("TRUNCATE TABLE Human");
                InsertHuman(context, testHuman);

                string firstName, lastName;
                context.Procedures.GetHumanViaOutput(100, out firstName, out lastName);
                Assert.AreEqual(firstName, testHuman.FirstName);
                Assert.AreEqual(lastName, testHuman.LastName);
            }
        }

        [Test]
        public void VariousOutputs()
        {
            using (var context = CreateSqlContext())
            {
                Human testHuman = new Human { Id = 100, FirstName = "Josef", LastName = "Kobzon" };
                context.Execute("TRUNCATE TABLE Human");
                InsertHuman(context, testHuman);

                long id;
                string firstName, lastName;
                context.Procedures.GetRandomHumanViaOutput(out id, out firstName, out lastName);
                Assert.AreEqual(id, testHuman.Id);
                Assert.AreEqual(firstName, testHuman.FirstName);
                Assert.AreEqual(lastName, testHuman.LastName);
            }
        }
    }
}