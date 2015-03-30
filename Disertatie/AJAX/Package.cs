using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Disertatie.AJAX
{
    public class Package
    {
        /// <summary>
        /// indicates if a error occurred
        /// </summary>
        public bool Error;

        /// <summary>
        /// indicates the error message
        /// </summary>
        public string ErrorMessage;

        public bool SessionExpired; //TODO: fix

        public Package()
        {
        }
    }
}