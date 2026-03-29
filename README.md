# tonl-net
TONL (Token-Optimized Notation Language) library for .NET


## Clarifications

- docs point out to single-line objects for simple objects,but js implementation does not follow that rule (always uses multi-line)
- what to do with single-precision floating point number?
- empty objects: shall they be encoded as `root:` or `root{}`? (spec example uses former, js implementation uses latter)
- array elements are separated by command and space, but js implementation uses only comma
- missing values in array of objects: spec says to have empty value between delimiters, but js implementation uses different colums per item
	```json
	{
		"array": [
			{ "a": 1, "b": 2 },
			{ "a": 3, "c": 4 }
		]
	}
	```
	spec encoding:
	```
	#version 1.0
	root:
	  array[2]{a, b, c}:
	    1, 2,
	    3,,4
	```
	js encoding:
	```
	#version 1.0
	root:
	  array[2]:
	    [0]{a,b}
	      a: 1
	      b: 2
	    [1]{a,c}
	      a: 3
	      c: 4
	```
 - in "Transformation Examples" example 1.2, when strings contain quotes, the example says that quotes
should be duplicated, but the js implementation escapes with a backslash inside the quoted string
- according to "Transformation Examples" example 2.1, root should be missing  and zero-level should be the user,
but js implementation maintains the root
- according to "Transformation Examples" example 2.2, flat objects should be single lined, but in the js implementation
are multi-line
- according to "Transformation Examples" example 2.3, flat objects should be one-liners, and version should not be quoted
but js version multilines objects and quotes version
- according to "Transformation Examples" example 3.1, long lines of arrays should be moved to next line,
but in the js library long arrays are also one-liners.
- Two-line behavior for logn arrays is also mentioned in IMPLEMENTATION_REFERENCE, rule 3, but discarded in js implementation
- 