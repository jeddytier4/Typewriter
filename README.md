# TypewriterX
a [Typewriter](https://github.com/frhagn/Typewriter) fork that adds new features

![#f03c15](https://placehold.it/15/f03c15/000000?text=+) `before trying this extension, please uninstall the original Typewriter extension since they *will conflict* on .tst files` 


[Documentation](http://avilv.github.io/TypewriterX)  
[Download](https://github.com/avilv/TypewriterX/releases)

New Features:

- [x] Entire solution processing
- [x] Multiple outputs from single template
- [x] Template Code Formatting
- [x] Expanded Type and Class types for better 'generic' support (AsClass,AsEnum, GenericDefClass)
- [x] tstx parser is extra conservative about newlines

## Documentation
documentation is still WIP

Step 1: Add a TypeScript Template file (.tstx)  
Step2: this is a .tstx example using the new syntax to output all classes and enums used on a webapi controller to seperate files, including typescript import statements. (no barreling)
```c#
${
	using Typewriter.Extensions.Types;
	using System.Text.RegularExpressions;
	using System.Collections;
	using System.Text;

	string TSPropertyName(Property p)
	{
	    return p.name.Replace("_", "");
	}

	static string _debug;
	void WriteDebug(string value)
	{
	    _debug += value + "\r\n";
	}

	string GetDebug(RootContext r)
	{
	    return _debug;
	}


	Type unwrapType(Type type)
	{
	    if (type.IsEnumerable && type.TypeArguments.Count > 0)
	    {
	        return type.TypeArguments.First();
	    }
	    return type;
	}

	bool IsClass(Type t)
	{
	    return !t.IsPrimitive && (!t.Namespace?.StartsWith("System") ?? true);
	}


	string TSClassName(Class c)
	{
	    string name;
	    if (c.IsGeneric)
	    {
	        name = $"{c.Name}<{string.Join(",", c.TypeParameters.Select(p => p.Name))}>";
	    }
	    else
	    {
	        name = c.Name;
	    }
	    return name;
	}

	bool HasBaseClass(Class c)
	{
	    return c.BaseClass != null;
	}


	Class GetTypeAsClass(Type t)
	{
	    var @type = unwrapType(t);
	    if (!IsClass(@type)) return null;
	    var @class = @type.AsClass;
	    if (@class == null) return null;
	    return @class;
	}

	string NormalizeTypeName(Type t) 
	{
	  var name = t.FullName;
	  if (t.IsNullable) {
	    name = name.TrimEnd('?');
	  }
	  return name;
	}

	string NormalizeClassName(Class c)
	{
	  var name = c.FullName;
	  if (c.IsGeneric) {
	    name = c.GenericDefClass.FullName;
	  }
	  return name;
	}

	IEnumerable<Class> DependantMethodsClasses(Class @class)
	{
	    HashSet<string> ignoredClasses = new HashSet<string>();
	    var methodsClasses = GetDependantClasses(@class, 1, includeBase: false, byProperties: false, byMethods: true, ignoredClasses: ignoredClasses).ToArray();

	    return methodsClasses;
	}

	IEnumerable<Enum> DependantMethodsEnums(Class @class)
	{
	    HashSet<string> ignoredEnums = new HashSet<string>();

	    var methodEnums = GetDependantEnums(@class, byProperties: false, byMethods: true, ignoredEnums: ignoredEnums).ToArray();

	    return methodEnums;
	}


	IEnumerable<Class> DependantMethodsClassesDeep(Class @class)
	{
	    HashSet<string> ignoredClasses = new HashSet<string>();
	    var methodsClasses = GetDependantClasses(@class, 1, includeBase: false, byProperties: false, byMethods: true, ignoredClasses: ignoredClasses).ToArray();
	    var dependancies = GetDependantClasses(methodsClasses, 0, includeBase: true, byProperties: true, byMethods: false, ignoredClasses: ignoredClasses).ToArray();
		
	    return methodsClasses.Union(dependancies);
	}

	IEnumerable<Enum> DependantMethodsEnumsDeep(Class @class)
	{
	    HashSet<string> ignoredEnums = new HashSet<string>();

	    var methodEnums = GetDependantEnums(@class, byProperties: false, byMethods: true, ignoredEnums: ignoredEnums).ToArray();

	    var methodsClasses = DependantMethodsClassesDeep(@class);
	    var dependanciesEnums = GetDependantEnums(methodsClasses, byProperties: true, byMethods: false, ignoredEnums: ignoredEnums).ToArray();

	    return methodEnums.Union(dependanciesEnums);
	}


	IEnumerable<Class> DependantClasses(Class @class)
	{
	   return GetDependantClasses(@class, 1);
	}

	IEnumerable<Enum> DependantEnums(Class @class)
	{
	   return GetDependantEnums(@class);
	}

	IEnumerable<Class> GetDependantClasses(Type t, HashSet<string> ignoredClasses)
	{
	    var typeClass = GetTypeAsClass(t);
	    if (typeClass == null) yield break;


	    if (typeClass.IsGeneric)
	    {
	        foreach (var typeArg in typeClass.TypeArguments)
	        {
	            var typeClassArg = GetTypeAsClass(typeArg);
	            if (typeClassArg == null || ignoredClasses.Contains(NormalizeClassName(typeClassArg))) continue;
	            ignoredClasses.Add(NormalizeClassName(typeClassArg));
	            yield return typeClassArg;
	        }

			var genericDef = typeClass.GenericDefClass;
			if (!ignoredClasses.Contains(NormalizeClassName(genericDef)))
			{
				ignoredClasses.Add(NormalizeClassName(genericDef));
				yield return genericDef;
			}

	    } else 
		{
			if (!ignoredClasses.Contains(NormalizeClassName(typeClass)))
			{
				ignoredClasses.Add(NormalizeClassName(typeClass));
				yield return typeClass;
			}
		}
	}

	IEnumerable<Class> GetDependantClasses(IEnumerable<Class> classes, int maxDepth, bool includeBase = true, bool byProperties = true, bool byMethods = false, HashSet<string> ignoredClasses = null)
	{
	    ignoredClasses = ignoredClasses ?? new HashSet<string>();

	    foreach (var @class in classes)
	    {
	        foreach (var dep in GetDependantClasses(@class, maxDepth, includeBase, byProperties, byMethods, ignoredClasses))
	        {
	            yield return dep;
	        }
	    }
	}

	IEnumerable<Class> GetDependantClasses(Class @class, int maxDepth, bool includeBase = false, bool byProperties = true, bool byMethods = false, HashSet<string> ignoredClasses = null)
	{

	    ignoredClasses = ignoredClasses ?? new HashSet<string>();


	    Stack<Tuple<Class, int>> stack = new Stack<Tuple<Class, int>>();
	    stack.Push(new Tuple<Class, int>(@class, 1));

	    while (stack.Count > 0)
	    {
	        var stackItem = stack.Pop();
	        var c = stackItem.Item1;
	        var depth = stackItem.Item2;

	        if (byProperties)
	            foreach (var prop in c.Properties)
	            {
	                foreach (var propClass in GetDependantClasses(prop.Type, ignoredClasses))
	                {
	                    yield return propClass;
	                    if (depth < maxDepth || maxDepth == 0)
	                    {
	                        stack.Push(new Tuple<Class, int>(propClass, depth + 1));
	                    }
	                }
	            }

	        if (byMethods)
	            foreach (var meth in c.Methods)
	            {
	                foreach (var propClass in GetDependantClasses(meth.Type, ignoredClasses))
	                {
	                    yield return propClass;
	                    if (depth < maxDepth || maxDepth == 0)
	                    {
	                        stack.Push(new Tuple<Class, int>(propClass, depth + 1));
	                    }
	                }

	                foreach (var parm in meth.Parameters)
	                {
	                    foreach (var parmClass in GetDependantClasses(parm.Type, ignoredClasses))
	                    {
	                        yield return parmClass;
	                        if (depth < maxDepth || maxDepth == 0)
	                        {
	                            stack.Push(new Tuple<Class, int>(parmClass, depth + 1));
	                        }
	                    }
	                }

	            }

	        if (includeBase)
	            if (c.BaseClass != null)
	            {
	                if (!ignoredClasses.Contains(NormalizeClassName(c.BaseClass)))
	                {
	                    ignoredClasses.Add(NormalizeClassName(c.BaseClass));
	                    stack.Push(new Tuple<Class, int>(c.BaseClass, depth + 1));
	                    yield return c.BaseClass;
	                }
	            }
	    }
	}


	IEnumerable<Enum> GetDependantEnums(Type t, HashSet<string> ignoredEnums)
	{
	    if (t.IsGeneric)
	    {
	        var typeClass = GetTypeAsClass(t);
	        if (typeClass == null) yield break;
	        foreach (var typeArg in typeClass.TypeArguments)
	        {
	            if (typeArg.IsEnum)
	            {
	                if (!ignoredEnums.Contains(NormalizeTypeName(typeArg)))
	                {
	                    ignoredEnums.Add(NormalizeTypeName(typeArg));
	                    yield return typeArg.AsEnum;
	                }
	            }
	        }
	    }
	    else if (t.IsEnum)
	    {
	        if (!ignoredEnums.Contains(NormalizeTypeName(t)))
	        {
	            ignoredEnums.Add(NormalizeTypeName(t));
	            yield return t.AsEnum;
	        }
	    }
	}

	IEnumerable<Enum> GetDependantEnums(IEnumerable<Class> classes, bool byProperties = true, bool byMethods = false, HashSet<string> ignoredEnums = null)
	{
	    ignoredEnums = ignoredEnums ?? new HashSet<string>();

	    foreach (var @class in classes)
	    {
	        foreach (var dep in GetDependantEnums(@class, byProperties, byMethods, ignoredEnums))
	        {
	            yield return dep;
	        }
	    }
	}

	IEnumerable<Enum> GetDependantEnums(Class @class, bool byProperties = true, bool byMethods = false, HashSet<string> ignoredEnums = null)
	{
	    ignoredEnums = ignoredEnums ?? new HashSet<string>();

	    if (byProperties)
	        foreach (var prop in @class.Properties)
	        {
	            foreach (var propEnum in GetDependantEnums(prop.Type, ignoredEnums))
	            {
	                yield return propEnum;
	            }
	        }

	    if (byMethods)
	        foreach (var meth in @class.Methods)
	        {
	            foreach (var propEnum in GetDependantEnums(meth.Type, ignoredEnums))
	            {
	                yield return propEnum;
	            }

	            foreach (var parm in meth.Parameters)
	            {
	                foreach (var parmEnum in GetDependantEnums(parm.Type, ignoredEnums))
	                {
	                    yield return parmEnum;
	                }
	            }

	        }
	}

	public static string FirstCharacterToLower(string str)
	{
		if (String.IsNullOrEmpty(str) || Char.IsLower(str, 0))
			return str;

		return Char.ToLowerInvariant(str[0]) + str.Substring(1);
	}

	string EnumName(Enum c)
	{
	     return c.Name;
	}

	string EnumFileName(Enum c)
	{
	    return FirstCharacterToLower(EnumName(c)).Replace("Model","") + ".model";
	}


	string ModelName(Class c)
	{
	     return c.Name;
	}

	string ModelFileName(Class c)
	{
	    return FirstCharacterToLower(ModelName(c)).Replace("Model","") + ".model";
	}

	string ServiceName(Class c)
	{
	    return c.Name.Replace("ApiController", "DataService");
	}

	string ServiceFileName(Class c)
	{
	    return c.name.Replace("ApiController","") + "Data.service";
	}

}
$Classes(:ApiController)<$ServiceFileName.ts>[
import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from 'rxjs';
$DependantMethodsClasses[import { $Name } from './$ModelFileName';]
$DependantMethodsEnums[import { $Name } from './$EnumFileName';]

@Injectable()
export class $ServiceName {
	constructor(private http: HttpClient) { }

$Methods[
    public $name($Parameters[$name: $Type][, ]): Observable<$Type> {
		return this.http.get();
	}
]
}
]
$Classes(:ApiController)[
$DependantMethodsEnumsDeep<$name.ts>[
export enum $Name 
{
  $Values[$Name = "$name"][,
  ]
}
]
$DependantMethodsClassesDeep<$ModelFileName.ts>[
$DependantClasses[import { $Name } from './$ModelFileName';]
$DependantEnums[import { $Name } from './$EnumFileName';]

export interface $TSClassName $HasBaseClass[extends $BaseClass ][]{
$Properties[
    $name: $Type;
]
}
]
]
]
```
