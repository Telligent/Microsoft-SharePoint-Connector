using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Caching;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Data.Cache
{
    public class SPCache
    {
        MemoryCache _cache;

        public SPCache()
        {
            _cache = new MemoryCache("Telligent.Evolution.Extensions.SharePoint.Components");
        }

        public void Put(string key, object value, int cacheDurationSeconds)
        {
            if (_cache.Contains(key))
                _cache.Remove(key);

            _cache.Add(key, value, DateTime.Now.AddSeconds(cacheDurationSeconds));
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public object Get(string key)
        {
            return _cache.Get(key);
        }

        public void Clear()
        {
            var list = new List<KeyValuePair<string, object>>(_cache);
            foreach (var item in list)
            {
                _cache.Remove(item.Key);    
            }
        }
        public static string ToMd5(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            
            return string.Join("", hash.Select(h => h.ToString("X2")));
        }
    }
}
