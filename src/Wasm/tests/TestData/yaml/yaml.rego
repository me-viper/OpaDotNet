package yaml.support

fixture := `
---
openapi: "3.0.1"
info:
  title: test
paths:
  /path1:
    get:
      x-amazon-apigateway-integration:
        type: "mock"
        httpMethod: "GET"
x-amazon-apigateway-policy:
  Version: "2012-10-17"
  Statement:
    - Effect: Allow
      Principal:
        AWS: "*"
      Action:
        - 'execute-api:Invoke'
      Resource: '*'
`

canParseYAML {
	resource := yaml.unmarshal(fixture)
	resource.info.title == "test"
}

hasSemanticError {
	yaml.unmarshal("a:\n\t1\nb:\n\t2\n")
}

hasSyntaxError {
	yaml.unmarshal("{ , }\n---\n{ 123,,, }\n")
}

hasReferenceError {
	yaml.unmarshal("{ , }\n---\n{ 123,,, }\n")
}

hasYAMLWarning {
	yaml.unmarshal("%FOO\n---bar\n")
}

# Results match OPA: the first document is returned, the rest of the stream still has to parse.
canUnmarshalMultiDocument {
	yaml.unmarshal("a: 1\n---\nb: 2\n") == {"a": 1}
	yaml.unmarshal("a: 1\n---\n") == {"a": 1}
}

hasSyntaxErrorInSecondDocument {
	yaml.unmarshal("a: 1\n---\nfoo: {")
}

canMarshalYAML[x] {
	string := yaml.marshal(input)
	x := yaml.unmarshal(string)
}

isValidYAML {
	yaml.is_valid(fixture) == true
	yaml.is_valid("foo: {") == false
	yaml.is_valid("{\"foo\": \"bar\"}") == true
	yaml.is_valid("a: 1\n---\nb: 2\n") == true
	yaml.is_valid("a: 1\n---\n---\n") == true
	yaml.is_valid("a: 1\n---\nfoo: {") == false
}