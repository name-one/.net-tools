using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InoSoft.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InoSoft.Tools.Data.Test
{
    [TestClass]
    public class SqlContextTest
    {
        [TestMethod]
        public void GetHumans()
        {
            var context = CreateSqlContext();
            var res = context.ProceduresProxy.GetHumans();
        }

        private SqlContext<IProceduresProxy> CreateSqlContext()
        {
            return new SqlContext<IProceduresProxy>("data source=(local);initial catalog=InoSoft.Tools.Data.Test;integrated security=true");
        }
    }
}