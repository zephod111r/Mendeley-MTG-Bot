using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudWorkerRole
{
    class AccessToken
    {
        public AccessToken(DateTime expires, object token)
        {
            expirationTimeStamp = expires;
            accessToken = token;
        }

        public DateTime expirationTimeStamp;
        public object accessToken;
    }
}
