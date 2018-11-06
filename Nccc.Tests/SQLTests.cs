using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;

namespace Nccc.Tests
{
    [TestClass]
    public class SQLTests
    {
        public const string sql = @"
create table MESSAGE_NOTIFY_TASK
(
   PK_ID                varchar2(32)         not null,
   OBJECT_ID            varchar2(32)         not null,
   MSG_TYPE             varchar2(32)         not null,
   MSG_CONTENT          varchar2(1024)       default null,
   PLAN_SEND_TIME       date                 not null,
   constraint PK_MESSAGE_NOTIFY_TASK1 primary key (OBJECT_ID),
   constraint PK_MESSAGE_NOTIFY_TASK primary key (HA_ID),
   constraint PK_MESSAGE_NOTIFY_TASK primary key (MSG_TYPE),
   HEHE varchar2(32) primary key not null,
   pLAN_SEND_TIME       date                 not null,
   HAHA varchar2(32) not null default null
);

comment on table MESSAGE_NOTIFY_TASK is
'消息通知任务';

comment on column MESSAGE_NOTIFY_TASK.PK_ID is
'主键';

comment on column MESSAGE_NOTIFY_TASK.OBJECT_ID is
'关联对象Id';

comment on column MESSAGE_NOTIFY_TASK.MSG_TYPE is
'消息类型';

comment on column MESSAGE_NOTIFY_TASK.MSG_CONTENT is
'消息内容';

comment on column MESSAGE_NOTIFY_TASK.PLAN_SEND_TIME is
'计划发送时间';

create unique index MESSAGE_NOTIFY_TASK_IDX on MESSAGE_NOTIFY_TASK (
   OBJECT_ID ASC,
   MSG_TYPE ASC
);

grant select, update, insert, delete on MESSAGE_NOTIFY_TASK  to dsp_app;
create synonym dsp_app.MESSAGE_NOTIFY_TASK  for dsp_adm.MESSAGE_NOTIFY_TASK ;

";
        private static NcParser _GetSqlParser()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return NcParser.LoadFromAssembly(assembly, "Nccc.Tests.sql.grammer", p =>
            {
                p.CaseSensitive = false;
                p.Scanner.Delims = new string[] { "(", ")", "[", "]", "{", "}", ",", ".", ";" };
                p.Scanner.QuotationMarks = new string[] { "\'" };
                p.Scanner.LineComment = new string[] { "--" };
                p.Scanner.CommentStart = "/*";
                p.Scanner.CommentEnd = "*/";
                p.Scanner.LispChar = new string[] { };
            });
        }

        [TestMethod]
        public void Test()
        {
            var parser = _GetSqlParser();
            var parseResult = parser.ScanAndParse(sql);
            Console.WriteLine(parseResult.ToSExp().ToPrettyString());
            Assert.IsTrue(parseResult.IsSuccess());
        }

        [TestMethod]
        public void Test100Times()
        {
            var times = 100;
            var parser = _GetSqlParser();
            for (var i = 0; i < times; ++i)
            {
                var parseResult = parser.ScanAndParse(sql);
                Assert.IsTrue(parseResult.IsSuccess());
            }
        }
    }
}
