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