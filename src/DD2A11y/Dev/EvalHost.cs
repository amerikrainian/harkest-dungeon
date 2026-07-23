using System;
using System.IO;
using System.Text;
using Mono.CSharp;

namespace DD2A11y.Dev {
    /// <summary>
    /// The /eval REPL: Mono.CSharp compiling against everything loaded in the process. Main-thread
    /// only. Mono.CSharp is expression-oriented - multi-statement code must be wrapped in an
    /// invoked lambda; state (variable declarations) persists across calls.
    /// </summary>
    public sealed class EvalHost {
        private Evaluator _eval;
        private readonly StringBuilder _report = new StringBuilder();

        private void Ensure() {
            if (_eval != null) {
                return;
            }
            var settings = new CompilerSettings();
            _eval = new Evaluator(new CompilerContext(settings, new StreamReportPrinter(new StringWriter(_report))));
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.IsDynamic) {
                    continue;
                }
                string name = assembly.GetName().Name;
                // The evaluator loads mscorlib/System/System.Core/System.Xml itself; facade
                // assemblies re-export the corlib types. Referencing either kind again makes
                // every System type ambiguous (CS0433).
                if (name == "mscorlib" || name == "System" || name == "System.Core" || name == "System.Xml"
                    || name == "netstandard" || name == "System.Runtime"
                    || name == "System.Runtime.InteropServices.RuntimeInformation" || !seen.Add(name)) {
                    continue;
                }
                try {
                    _eval.ReferenceAssembly(assembly);
                } catch (Exception ex) {
                    Plugin.Log.LogWarning("eval: could not reference " + name + ": " + ex.Message);
                }
            }
            _eval.Run("using System; using System.Linq; using System.Collections.Generic; using UnityEngine;");
        }

        public string Run(string code) {
            Ensure();
            _report.Length = 0;
            object result;
            bool resultSet;
            string leftover;
            try {
                leftover = _eval.Evaluate(code, out result, out resultSet);
            } catch (Exception ex) {
                return "exception: " + ex;
            }
            if (_report.Length > 0) {
                return "compile:\n" + _report;
            }
            if (leftover != null) {
                return "incomplete input (Mono.CSharp needs expression form; wrap statements in an invoked Func)";
            }
            return resultSet ? (result?.ToString() ?? "null") : "ok";
        }

        /// <summary>Compile a bool expression once for per-frame /wait evaluation. Null (with an
        /// error) when it does not compile.</summary>
        public CompiledMethod CompileExpression(string expression, out string error) {
            Ensure();
            _report.Length = 0;
            try {
                var compiled = _eval.Compile(expression);
                error = _report.Length > 0 ? _report.ToString() : null;
                return compiled;
            } catch (Exception ex) {
                error = ex.ToString();
                return null;
            }
        }
    }
}
