using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.API
{
    public class RequestResult
    {

        private int returnCode;
        public int ReturnCode => returnCode;
        private string msg;
        public string Msg => msg;

        public bool IsSuccess => returnCode == 1 || returnCode == 2;
        /// <summary>
        /// 1:info 2:success 3:warning 4:error
        /// </summary>
        public RequestResult(int returnCode, string msg)
        {
            this.returnCode = returnCode;
            this.msg = msg;
        }
    }

    public class RequestResult<T> : RequestResult
    {
        private T? obj;
        public T? Obj => obj;
        public RequestResult(int returnCode, string msg, T? obj) : base(returnCode, msg)
        {
            this.obj = obj;
        }
    }
}
