using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods
{
    public interface IEncrypted
    {
        void UpdateEncryptedFields(object obj);
        void InvokeEncryption();
    }
}
