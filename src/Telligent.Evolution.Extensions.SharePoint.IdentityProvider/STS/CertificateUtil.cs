﻿using System;
using System.Security.Cryptography.X509Certificates;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider.STS
{
    public class CertificateUtil
    {
        public static X509Certificate2 GetCertificate(StoreName name, StoreLocation location, string subjectName)
        {
            var store = new X509Store(name, location);

            X509Certificate2Collection certificates = null;
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2 result = null;
                certificates = store.Certificates;

                foreach (var cert in certificates)
                {
                    if (!string.IsNullOrEmpty(cert.SubjectName.Name) && cert.SubjectName.Name.Equals(subjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (result != null)
                        {
                            throw new ApplicationException(string.Format("There are multiple certificates for subject Name {0}", subjectName));
                        }

                        result = new X509Certificate2(cert);
                    }
                }
                if (result == null)
                {
                    throw new ApplicationException(string.Format("No certificate was found for subject Name {0}", subjectName));
                }

                return result;
            }
            finally
            {
                if (certificates != null)
                {
                    foreach (var cert in certificates)
                    {
                        cert.Reset();
                    }
                }

                store.Close();
            }
        }
    }
}
