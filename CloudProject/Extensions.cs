using System;
using System.Net;
using System.Threading;
using System.IO;
using System.Reflection;

namespace CloudStorage_extensions
{
	public static class ExtensionMethods
	{
		public static WebResponse GetResponse(this WebRequest request){
			ManualResetEvent evt = new ManualResetEvent (false);
			WebResponse response = null;
            Exception ex = null;
			request.BeginGetResponse ((IAsyncResult ar) => {
                try
                {
                    response = request.EndGetResponse(ar);                    
                }
                catch (Exception e)
                {
                    ex = e;
                }
                evt.Set();
			}, null);
			evt.WaitOne ();
            if (ex != null)
                throw ex; //throw on this thread
			return response as WebResponse;
		}

		public static Stream GetRequestStream(this WebRequest request){
			ManualResetEvent evt = new ManualResetEvent (false);
			Stream requestStream = null;
            Exception ex = null;
			request.BeginGetRequestStream ((IAsyncResult ar) => {
                try
                {
                    requestStream = request.EndGetRequestStream(ar);
                }
                catch (Exception e)
                {
                    ex = e;
                }
                evt.Set();
			}, null);
			evt.WaitOne ();
            if (ex != null)
                throw ex; //throw on this thread
			return requestStream;
		}

        public static void SetHeader(this WebRequest request, string header, string value)
        {
            // Retrieve the property through reflection.
            PropertyInfo PropertyInfo = request.GetType().GetProperty(header.Replace("-", string.Empty));
            // Check if the property is available.
            if (PropertyInfo != null)
            {
                PropertyInfo.SetValue(request, value, null);
            }
            else
            {
                request.Headers[header] = value;
            }
        }
	}
}

