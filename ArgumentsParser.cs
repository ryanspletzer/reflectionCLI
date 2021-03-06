using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace reflectionCli {

    public static class ArgumentsParser {
        public static object[] ParseArgumentsFromString(string input, Type type, ref ConstructorInfo constructor) {
            List<object> outval = new List<object>();

            //no input case
            var parts = input.Split(new char[] { ' ' }, 2);
            if (parts.Length < 2) {
                 var constructorlist = type.GetConstructors().Where(x => (x.GetParameters().Count() == 0)).ToList();

                if (constructorlist.Count == 0) {
                    throw new Exception($"No Constructors for {type.Name} have 0 arguments {Environment.NewLine}");
                }

                constructor = constructorlist[0];

                return null;
            }

            string argstring = parts[1];
            var atoms = Regex.Matches(argstring, "(\\-\\S*)|(\\[.+?\\])|(\\\".+?\\\")|(\\S*)").Cast<Match>().Where(x => !String.IsNullOrEmpty(x.Value));

            var paramnames = atoms.Where(x => (x.Value[0] == '-'));

            if (paramnames == null) {
                throw new Exception("No Parameter Names were found. Please use the convention: -ParameterName Value");
            }

            //see if any of the parameter counts match given inputs
            var validconstructors = type.GetConstructors()
                                        .Where(x => (x.GetParameters().Count() == paramnames.Count()));

            if (validconstructors == null) {
                throw new Exception($"No Constructors for {type.Name} have {paramnames.Count()} arguments {Environment.NewLine}");
            }

            //break input into groups based on the variable name that came before them
            Dictionary<Match, List<string>> parampackages = new Dictionary<Match, List<string>>();
            for (int i = 0; i < paramnames.Count(); i++)
            {
                List<string> objs = new List<string>();
                if (i == paramnames.Count() - 1) {
                    atoms.ToList().Where(x => ((x.Index > paramnames.ToList()[i].Index)))
                                    .ToList()
                                    .ForEach(x => objs.Add(x.Value));
                }
                else {
                    atoms.ToList().Where(x => ((x.Index > paramnames.ToList()[i].Index) &&
                                                (x.Index < paramnames.ToList()[i+1].Index)))
                                    .ToList()
                                    .ForEach(x => objs.Add(x.Value));
                }

                //strip quotes off of things that start and end with them
                for (int n = 0; n < objs.Count(); n++) {
                    if (objs[n][0] == '"' && objs[n].Last() == '"') {
                        objs[n] = objs[n].Substring(1, objs[n].Length - 2);
                    }
                }

                parampackages.Add(paramnames.ToList()[i], objs);
            }

            //find constructor whos vartiable names match those given
            var matchingconstructors = type.GetConstructors()
                                            .Where(c => c.GetParameters()
                                                         .Select(x => x.Name)
                                                         .Intersect(parampackages.Select(y => y.Key.Value.Remove(0,1))).Count() == parampackages.Count()
                                                         &&
                                                         parampackages.Select(y => y.Key.Value.Remove(0,1))
                                                         .Intersect(c.GetParameters().Select(x => x.Name)).Count() == c.GetParameters().Count()
                                                  );

            if (matchingconstructors == null) {
                throw new Exception($"No Constructors for {type.Name} have matching input names to those Provided.");
            }

            if (matchingconstructors.Count() > 1) {
                throw new Exception($"Multiple Constructors for {type.Name} have matching input names, this is an issue with the way that {type.Name} was written.");
            }

            ConstructorInfo chosenconstructor = matchingconstructors.ToList()[0];
            for (int i = 0; i < chosenconstructor.GetParameters().Count(); i++) {
                Type outtype = chosenconstructor.GetParameters().ToList()[i].ParameterType;
                var tempobj = parampackages.Where(x => (x.Key.Value.Remove(0,1) == chosenconstructor.GetParameters().ToList()[i].Name))
                                            .Select(y => y.Value).ToList()[0];

                if (tempobj.Count() == 1 && !outtype.IsArray && !outtype.GetInterfaces().Contains(typeof(System.Collections.IList))) {
                    outval.Add(Convert.ChangeType(tempobj.ToArray()[0], outtype));
                }
                else {
                    Type nesttype = outtype.GetTypeInfo().GenericTypeArguments[0];
                    switch (nesttype.Name) {

                        case "Int32":
                            outval.Add(tempobj.Select(x => Convert.ToInt32(x)).ToList());
                            break;

                        case "Double":
                            outval.Add(tempobj.Select(x => Convert.ToDouble(x)).ToList());
                            break;

                        case "Boolean":
                            outval.Add(tempobj.Select(x => Convert.ToBoolean(x)).ToList());
                            break;

                        case "Decimal":
                            outval.Add(tempobj.Select(x => Convert.ToDecimal(x)).ToList());
                            break;

                        case "DateTime":
                            outval.Add(tempobj.Select(x => Convert.ToDateTime(x)).ToList());
                            break;

                        case "Byte":
                            outval.Add(tempobj.Select(x => Convert.ToByte(x)).ToList());
                            break;

                        default:
                            outval.Add(tempobj);
                            break;
                    }

                    //dynamic converted = new object[0];
                    //converted = Convert.ChangeType(converted, outtype);
                    //converted = tempobj.Select(x => Convert.ChangeType(x, nesttype)).ToList();
                    //outval.Add(tempobj.Select(x => Convert.ChangeType(x, nesttype)).ToArray());
                    //outval.Add(Convert.ChangeType(tempobj, outtype));
                }
            }

            constructor = chosenconstructor;
            return outval.ToArray();
        }
    }
}