using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldCompanyDataViewer.Utils
{
    public class DatabaseConextNullException : Exception
    {
        public DatabaseConextNullException()
        {
        }

        public DatabaseConextNullException(string message)
            : base(message)
        {
        }

        public DatabaseConextNullException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
