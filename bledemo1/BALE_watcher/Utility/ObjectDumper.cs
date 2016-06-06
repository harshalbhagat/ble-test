//using System;
//using System.Collections;
//using System.Diagnostics;
//using System.Globalization;
//using System.Reflection;

//namespace BALE_watcher.Dumper
//{
//    public class ObjectDumper
//    {
//        public static void Write(object element, int depth)
//        {
//            ObjectDumper dumper = new ObjectDumper(depth);
//            dumper.WriteObject(null, element);
//        }


//        int _pos;
//        int _level;
//        readonly int _depth;

//        private ObjectDumper(int depth)
//        {
//            _depth = depth;
//        }

//        private void Write(string s)
//        {
//            if (s != null)
//            {
//                Debug.Write(s);
//                _pos += s.Length;
//            }
//        }

//        private void WriteIndent()
//        {
//            for (int i = 0; i < _level; i++)
//                Debug.Write("  ");
//        }

//        private void WriteLine()
//        {
//            Debug.WriteLine("");
//            _pos = 0;
//        }

//        private void WriteTab()
//        {
//            Write("  ");
//            while (_pos % 8 != 0)
//            {
//                Write(" ");
//            }
//        }

//        private void WriteObject(string prefix, object element)
//        {
//            if (element == null || element is ValueType || element is string)
//            {
//                WriteIndent();
//                Write(prefix);
//                WriteValue(element);
//                WriteLine();
//            }
//            else
//            {
//                IEnumerable enumerableElement = element as IEnumerable;
//                if (enumerableElement != null)
//                {
//                    foreach (object item in enumerableElement)
//                    {
//                        if (item is IEnumerable && !(item is string))
//                        {
//                            WriteIndent();
//                            Write(prefix);
//                            Write("...");
//                            WriteLine();
//                            if (_level < _depth)
//                            {
//                                _level++;
//                                WriteObject(prefix, item);
//                                _level--;
//                            }
//                        }
//                        else
//                        {
//                            WriteObject(prefix, item);
//                        }
//                    }
//                }
//                else
//                {
//                    MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
//                    WriteIndent();
//                    Write(prefix);
//                    bool propWritten = false;
//                    foreach (MemberInfo m in members)
//                    {
//                        FieldInfo f = m as FieldInfo;
//                        PropertyInfo p = m as PropertyInfo;
//                        if (f == null && p == null)
//                        {
//                            continue;
//                        }

//                        // Eitan, this change was to make each property on a new line
//                        //if ( propWritten )
//                        //    {
//                        //    WriteTab();
//                        //    }
//                        //else
//                        //    {
//                        //    propWritten = true;
//                        //    }
//                        WriteTab();
//                        propWritten = true;

//                        Write(m.Name);
//                        Write("=");
//                        Type t = f != null ? f.FieldType : p.PropertyType;
//                        if (t.GetTypeInfo().IsValueType || t == typeof(string))
//                        {
//                            WriteValue(f != null ? f.GetValue(element) : p.GetValue(element, null));
//                        }
//                        else
//                        {
//                            Write(typeof(IEnumerable).IsAssignableFrom(t) ? "..." : "{ }");
//                        }

//                        // Eitan
//                        WriteLine();
//                    }

//                    if (propWritten)
//                    {
//                        WriteLine();
//                    }

//                    if (_level < _depth)
//                    {
//                        foreach (MemberInfo m in members)
//                        {
//                            var f = m as FieldInfo;
//                            var p = m as PropertyInfo;

//                            if (f == null && p == null)
//                            {
//                                continue;
//                            }

//                            Type t = f != null ? f.FieldType : p.PropertyType;
//                            if (t.GetTypeInfo().IsValueType || t == typeof(string))
//                            {
//                                continue;
//                            }

//                            object value = f != null ? f.GetValue(element) : p.GetValue(element, null);
//                            if (value == null)
//                            {
//                                continue;
//                            }

//                            _level++;
//                            WriteObject(m.Name + ": ", value);
//                            _level--;
//                        }
//                    }
//                }
//            }
//        }

//        private void WriteValue(object o)
//        {
//            if (o == null)
//            {
//                Write("null");
//            }
//            else if (o is DateTime)
//            {
//                Write(((DateTime)o).ToString(CultureInfo.InvariantCulture));
//            }
//            else if (o is ValueType || o is string)
//            {
//                Write(o.ToString());
//            }
//            else if (o is IEnumerable)
//            {
//                Write("...");
//            }
//            else
//            {
//                Write("{ }");
//            }
//        }
//    }
//}

