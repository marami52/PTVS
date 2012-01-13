/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System.Collections.Generic;
using System.Text;

namespace Microsoft.PythonTools.Parsing.Ast {

    public class CallExpression : Expression {
        private readonly Expression _target;
        private readonly Arg[] _args;

        public CallExpression(Expression target, Arg[] args) {
            _target = target;
            _args = args;
        }

        public Expression Target {
            get { return _target; }
        }

        public IList<Arg> Args {
            get { return _args; }
        } 

        public bool NeedsLocalsDictionary() {
            NameExpression nameExpr = _target as NameExpression;
            if (nameExpr == null) return false;

            if (_args.Length == 0) {
                if (nameExpr.Name == "locals") return true;
                if (nameExpr.Name == "vars") return true;
                if (nameExpr.Name == "dir") return true;
                return false;
            } else if (_args.Length == 1 && (nameExpr.Name == "dir" || nameExpr.Name == "vars")) {
                if (_args[0].Name == "*" || _args[0].Name == "**") {
                    // could be splatting empty list or dict resulting in 0-param call which needs context
                    return true;
                }
            } else if (_args.Length == 2 && (nameExpr.Name == "dir" || nameExpr.Name == "vars")) {
                if (_args[0].Name == "*" && _args[1].Name == "**") {
                    // could be splatting empty list and dict resulting in 0-param call which needs context
                    return true;
                }
            } else {
                if (nameExpr.Name == "eval") return true;
                if (nameExpr.Name == "execfile") return true;
            }
            return false;
        }

        internal override string CheckAssign() {
            return "can't assign to function call";
        }

        internal override string CheckDelete() {
            return "can't delete function call";
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (_target != null) {
                    _target.Walk(walker);
                }
                if (_args != null) {
                    foreach (Arg arg in _args) {
                        arg.Walk(walker);
                    }
                }
            }
            walker.PostWalk(this);
        }

        internal override void AppendCodeString(StringBuilder res, PythonAst ast) {
            _target.AppendCodeString(res, ast);
            res.Append(this.GetProceedingWhiteSpace(ast));
            var listWhiteSpace = this.GetListWhiteSpace(ast);
            res.Append('(');
            for (int i = 0; i < _args.Length; i++) {
                if (i > 0) {
                    if (listWhiteSpace != null) {
                        res.Append(listWhiteSpace[i - 1]);
                    }
                    res.Append(',');
                }

                _args[i].AppendCodeString(res, ast);
            }
            
            if (listWhiteSpace != null && listWhiteSpace.Length == _args.Length && _args.Length != 0) {
                // trailing comma
                res.Append(listWhiteSpace[listWhiteSpace.Length - 1]);
                res.Append(",");
            }

            if (!this.IsMissingCloseGrouping(ast)) {
                res.Append(this.GetSecondWhiteSpace(ast));
                res.Append(')');
            }
        }

        internal override string GetLeadingWhiteSpace(PythonAst ast) {
            return _target.GetLeadingWhiteSpace(ast);
        }
    }
}