using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace deploy.Models {
    public class LogService {
        private static Logger Logger { get; set; }

        static LogService() {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public static void Trace(string message) { Logger.Trace(message); }
        public static void Debug(string message) { Logger.Debug(message); }
        public static void Info(string message) { Logger.Info(message); }
        public static void Warn(string message) { Logger.Warn(message); }
        public static void Error(string message) { Logger.Error(message); }
        public static void Fatal(string message) { Logger.Fatal(message); }

        public static void Error(Exception e) {
            if(Logger.IsErrorEnabled)
                Logger.Error(e.GetLogMessage());
        }

        public static void Fatal(Exception e) {
            if(Logger.IsFatalEnabled)
                Logger.Fatal(e.GetLogMessage());
        }
    }

    public static class LogExtensions {
        public static string GetLogMessage(this Exception e) {
            string message = "";
            string newline = Environment.NewLine;

            if(e is HttpUnhandledException && e.InnerException != null) {
                e = e.InnerException;
            }

            HttpContext context = HttpContext.Current;

            if(context != null && context.Request != null) {
                if(context.User != null && context.User.Identity.IsAuthenticated) {
                    message += newline + "User: " + context.User.Identity.Name;
                }
                message += newline + "Requested URL: " + context.Request.Url;
                message += newline + "Referrer: " + context.Request.UrlReferrer;
                message += newline + "Client IP address: " + context.Request.UserHostAddress;
                message += newline + "User-agent: " + context.Request.UserAgent;
            }

            message += newline + e.ToString();

            return message;
        }
    }
}