using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ClientManagement.Services
{
    public class ServiceException : Exception
    {
        public ServiceException() : base() { }
        public ServiceException(string message) : base(message) { }
        public ServiceException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }

    public class ServiceFieldException : Exception
    {
        private string _field;

        public string Field
        {
            get { return _field; }
        }

        public ServiceFieldException(string field) : base()
        {
            _field = field;
        }
        public ServiceFieldException(string field, string message) : base(message)
        {
            _field = field;
        }
        public ServiceFieldException(string field, string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
            _field = field;
        }
    }
}
