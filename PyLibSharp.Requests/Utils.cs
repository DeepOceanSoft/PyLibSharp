﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PyLibSharp.Requests
{
    public static class Utils
    {
        public static string UnGzipFromStreamToString(Stream inputStream)
        {
            String ungzipped = "";
            using (GZipStream stream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    ungzipped = reader.ReadToEnd();
                }
            }

            return ungzipped;
        }

        public static string UnDeflateFromStreamToString(Stream inputStream)
        {
            String undeflated = "";
            using (DeflateStream stream = new DeflateStream(inputStream, CompressionMode.Decompress))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    undeflated = reader.ReadToEnd();
                }
            }

            return undeflated;
        }

        public static void UnGzipFromStreamToStream(Stream inputStream, Stream outputStream)
        {
            using (GZipStream stream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                stream.CopyTo(outputStream);
            }
        }

        public static void UnDeflateFromStreamToStream(Stream inputStream,Stream outputStream)
        {
            using (DeflateStream stream = new DeflateStream(inputStream, CompressionMode.Decompress))
            {
                stream.CopyTo(outputStream);
            }
        }

        /// <summary>
        /// 根据HTML头部中的meta标记来返回网页编码
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        public static Encoding GetHtmlEncodingByMetaHeader(string htmlContent)
        {
            Encoding responseEncoding = Encoding.UTF8;
            //通过HTML头部的Meta tag判断编码
            var CharSetMatch =
                Regex.Match(htmlContent, @"<meta.*?charset=""?([a-z0-9-]+)\b", RegexOptions.IgnoreCase)
                     .Groups;
            if (CharSetMatch.Count > 1 && CharSetMatch[1].Value != "")
            {
                string overrideCharset = CharSetMatch[1].Value;
                responseEncoding = Encoding.GetEncoding(overrideCharset);
            }

            return responseEncoding;
        }

        /// <summary>
        /// 链式获取父错误的每一级 InnerException，并作为 List 返回
        /// </summary>
        /// <param name="exOuter">父级错误</param>
        /// <returns></returns>
        public static List<Exception> GetInnerExceptionList(Exception exOuter)
        {
            List<Exception> lstRet = new List<Exception>();
            while (exOuter != null)
            {
                lstRet.Add(exOuter);
                exOuter = exOuter.InnerException;
            }

            return lstRet;
        }

        /// <summary>
        /// 链式获取父错误的每一级 InnerException 的错误消息，并拼接为字符串返回
        /// <para>每一行代表一级的错误消息，从上到下依次深入</para>
        /// </summary>
        /// <param name="exOuter">父级错误</param>
        /// <returns></returns>
        public static string GetInnerExceptionMessages(Exception exOuter)
        {
            StringBuilder sb = new StringBuilder();

            GetInnerExceptionList(exOuter).ForEach(i => sb.AppendLine(i.Message));

            return sb.ToString();
        }

        /// <summary>
        /// 通用错误处理函数
        /// </summary>
        /// <param name="useHandler">是否使用捕捉器</param>
        /// <param name="handler">捕捉器</param>
        /// <param name="innerException">内部错误</param>
        /// <param name="errType">错误类型</param>
        internal static void HandleError(bool      useHandler, EventHandler<Requests.AggregateExceptionArgs> handler,
                                         Exception innerException, ErrorType errType)
        {
            Debug.Print($"Requests库出现错误：({(useHandler ? "使用" : "未使用")}自定义错误捕捉器)" +
                        $"\r\n报错信息为："                                            +
                        $"\r\n{GetInnerExceptionMessages(innerException)}"       +
                        $"\r\n报错堆栈为："                                            +
                        $"\r\n{innerException.StackTrace}");

            if (useHandler)
            {
                handler?.Invoke(null,
                                new Requests.AggregateExceptionArgs()
                                {
                                    AggregateException =
                                        new AggregateException(innerException),
                                    ErrType = errType
                                });
            }
            else
            {
                throw innerException;
            }
        }

        public static CookieCollection GetAllCookies(CookieContainer cookieJar)
        {
            if (!(cookieJar is null) && cookieJar.Count > 0)
            {
                CookieCollection cookieCollection = new CookieCollection();

                Hashtable table = (Hashtable) cookieJar.GetType().InvokeMember("m_domainTable",
                                                                               BindingFlags.NonPublic |
                                                                               BindingFlags.GetField  |
                                                                               BindingFlags.Instance,
                                                                               null,
                                                                               cookieJar,
                                                                               new object[] { });

                foreach (var tableKey in table.Keys)
                {
                    String str_tableKey = (string) tableKey;

                    if (str_tableKey[0] == '.')
                    {
                        str_tableKey = str_tableKey.Substring(1);
                    }

                    SortedList list = (SortedList) table[tableKey].GetType().InvokeMember("m_list",
                        BindingFlags.NonPublic |
                        BindingFlags.GetField  |
                        BindingFlags.Instance,
                        null,
                        table[tableKey],
                        new object[] { });

                    foreach (var listKey in list.Keys)
                    {
                        String url = "http://" + str_tableKey + (string) listKey;
                        cookieCollection.Add(cookieJar.GetCookies(new Uri(url)));
                    }
                }

                return cookieCollection;
            }
            else
            {
                return new CookieCollection();
            }
        }
    }
}