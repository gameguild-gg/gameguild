# Text Formats: JSON and Beyond

## JSON (RFC 8259)

JSON (JavaScript Object Notation) is the dominant text interchange format. Defined by [RFC 8259](https://datatracker.ietf.org/doc/html/rfc8259) and visualized with railroad diagrams at [json.org](https://www.json.org/), it supports six value types:

```mermaid
flowchart TD
    V[JSON Value] --> O[Object: '{' key:value pairs '}']
    V --> A[Array: '[' values ']']
    V --> S["String: '\"text\"'"]
    V --> N[Number: integer or float]
    V --> B["Boolean: true / false"]
    V --> NL["Null: null"]
```

### JSON Example: Game Player

```json
{
  "id": 42,
  "position": {
    "x": 1.5,
    "y": 2.0,
    "z": 3.7
  },
  "health": 100,
  "inventory": ["sword", "shield", "potion"],
  "alive": true
}
```

This is **80+ bytes** for data that could be represented in **18 bytes** (or fewer) in binary.

### JSON Grammar Rules

The full grammar fits on a single page (a major design virtue):

- **Strings** must use double quotes (`"key"`, not `'key'`)
- **Numbers** can be integer or floating-point, no hex/octal/NaN/Infinity
- **No comments** — a deliberate design choice for simplicity
- **No trailing commas** — `{"a": 1,}` is invalid
- **Encoding** must be UTF-8 (RFC 8259 mandates this)

### JSON in C++: Boost.JSON

```cpp
#include <boost/json.hpp>
#include <iostream>

namespace json = boost::json;

// Serialize: C++ struct → JSON string
std::string player_to_json(const Player& p) {
    json::object obj;
    obj["id"] = p.id;
    obj["position"] = {
        {"x", p.x}, {"y", p.y}, {"z", p.z}
    };
    obj["health"] = p.health;
    return json::serialize(obj);
}

// Deserialize: JSON string → C++ struct
Player json_to_player(std::string_view input) {
    json::value val = json::parse(input);
    auto& obj = val.as_object();

    Player p;
    p.id = obj["id"].as_int64();
    p.x = obj["position"].as_object()["x"].as_double();
    p.y = obj["position"].as_object()["y"].as_double();
    p.z = obj["position"].as_object()["z"].as_double();
    p.health = obj["health"].as_int64();
    return p;
}
```

### JSON in C++: nlohmann/json

The most popular C++ JSON library with an intuitive, STL-like API:

```cpp
#include <nlohmann/json.hpp>

using json = nlohmann::json;

// Serialize with automatic conversion
json j;
j["id"] = 42;
j["position"] = {{"x", 1.5}, {"y", 2.0}, {"z", 3.7}};
j["health"] = 100;

std::string wire = j.dump();  // compact JSON string

// Deserialize
json parsed = json::parse(wire);
uint32_t id = parsed["id"];
float x = parsed["position"]["x"];
```

## JSON: Strengths and Weaknesses

| Strength                       | Weakness                             |
| ------------------------------ | ------------------------------------ |
| Human-readable / debuggable    | 2-10× larger than binary formats     |
| Self-describing (keys in data) | Parsing is CPU-intensive             |
| Universal language support     | No integer/float distinction in spec |
| Standard (RFC 8259)            | No schema — validation is manual     |
| Great for REST APIs            | Terrible for 60 Hz game state        |

::: warning "When NOT to use JSON"

If you're sending data at high frequency (game ticks, sensor data, financial feeds), JSON's overhead is prohibitive:

- **Size:** `{"x":1.5,"y":2.0,"z":3.7}` = 27 bytes vs 12 bytes (3 raw floats) vs 4 bytes (bitpacked)
- **Parse time:** String → number conversion, hash-map key lookups, UTF-8 validation
- **Allocations:** Dynamic memory for strings, arrays, objects

Use JSON for configuration, REST APIs, and debugging. Use binary formats for real-time data.

:::

## Other Text Formats

### CSV (RFC 4180)

The simplest structured text format — rows of comma-separated values:

```
id,x,y,z,health
42,1.5,2.0,3.7,100
43,5.0,0.0,1.2,80
```

- No types (everything is a string)
- No nesting (flat tables only)
- Delimiter ambiguity (`"field with, comma"` requires quoting)
- Still widely used for data export/import

### XML

```xml
<player id="42">
    <position x="1.5" y="2.0" z="3.7"/>
    <health>100</health>
</player>
```

- Extremely verbose (closing tags, attributes vs elements)
- Supports schemas (XSD) and namespaces
- Largely replaced by JSON in modern APIs
- Still used in legacy systems, SOAP, and some game engines (Unity metadata)

### YAML

```yaml
player:
  id: 42
  position:
    x: 1.5
    y: 2.0
    z: 3.7
  health: 100
```

- Superset of JSON (valid JSON is valid YAML)
- Indentation-based — human-friendly but whitespace-sensitive
- Complex spec with surprising behaviors (e.g., `no` → `false`, Norway problem)
- Popular for configs: Docker Compose, Kubernetes, GitHub Actions

### TOML

```toml
[player]
id = 42
health = 100

[player.position]
x = 1.5
y = 2.0
z = 3.7
```

- Explicit types (integers, floats, dates, arrays)
- Simpler than YAML, fewer surprises
- Growing adoption: Rust (`Cargo.toml`), Python (`pyproject.toml`)

## Text Format Comparison

| Format | Nesting | Types    | Schema   | Readability | Use Case             |
| ------ | ------- | -------- | -------- | ----------- | -------------------- |
| JSON   | Yes     | 6 types  | No (std) | Good        | APIs, interchange    |
| CSV    | No      | Strings  | No       | Excellent   | Tabular data, export |
| XML    | Yes     | Strings  | XSD      | Poor        | Legacy, SOAP         |
| YAML   | Yes     | Rich     | No       | Excellent   | Config files         |
| TOML   | Limited | Explicit | No       | Excellent   | Config files         |

::: tip "Choosing a text format"

- **Data interchange between services:** JSON
- **Configuration files:** TOML (simple) or YAML (complex)
- **Data export / spreadsheets:** CSV
- **Legacy systems / enterprise:** XML
- **Real-time network data:** None — use binary formats

:::
