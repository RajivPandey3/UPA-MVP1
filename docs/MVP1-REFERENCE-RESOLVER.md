# Reference Resolver v1.0

Implemented:
- AssetDatabase dependency graph
- resolved/unresolved edge state
- forward reference edges
- reverse-reference index
- stable deterministic ordering
- diagnostics
- EditorWindow
- Editor test fixture

Important:
`AssetDatabase.GetDependencies` gives a strong asset-level graph, but it does not
replace full serialized-property analysis. Later layers should add object-field,
component-field, scene-object and script-semantic references where Unity exposes them safely.
