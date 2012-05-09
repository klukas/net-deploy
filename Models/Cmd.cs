using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace deploy.Models {

	/// <summary>
	/// For running windows command-line commands.
	/// </summary>
	public class Cmd {
		string _command;
		string _runFrom;
		string _logPath;
		StringBuilder _output;
		object outputlock = new object();

		public Cmd(string command = null, string runFrom = null, string logPath = null) {
			_command = command;
			_runFrom = runFrom;
			_logPath = logPath;
			_output = new StringBuilder();
		}

		public CmdResult Run() {
			Process proc = new Process();

			proc.StartInfo.FileName = "cmd.exe";
			proc.StartInfo.Arguments = "/c " + _command;
			if(!string.IsNullOrEmpty(_runFrom)) proc.StartInfo.WorkingDirectory = _runFrom;

			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;

			proc.OutputDataReceived += new DataReceivedEventHandler(proc_OutputDataReceived);
			proc.ErrorDataReceived += new DataReceivedEventHandler(proc_ErrorDataReceived);

			proc.Start();

			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();

			proc.WaitForExit();

			var result = new CmdResult(_command, proc.ExitCode, _output.ToString());
			proc.Close();
			return result;
		}

		void proc_OutputDataReceived(object sender, DataReceivedEventArgs e) {
			if(!string.IsNullOrEmpty(e.Data)) {
				_output.AppendLine(e.Data);
				WriteLog(e.Data);
			}
		}

		void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
			if(!string.IsNullOrEmpty(e.Data)) {
				_output.AppendLine(e.Data);
				WriteLog(e.Data);
			}
		}

		void WriteLog(string line) {
			if(_logPath != null) {
				lock(outputlock) {
					File.AppendAllText(_logPath, line + "\r\n");
				}
			}
		}
	}

	public class CmdResult {
		public string Command { get; private set; }
		public int ExitCode { get; private set; }
		public string Output { get; private set; }

		public CmdResult(string command, int exitcode, string output) {
			Command = command;
			ExitCode = exitcode;
			Output = output;
		}

		public CmdResult EnsureCode(int code) {
			if(ExitCode != code) {
				throw new Exception("Exit code was " + ExitCode + ".\r\ncommand: " + Command + "\r\noutput: " + Output);
			}
			return this;
		}
	}
}