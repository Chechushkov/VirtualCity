import json

import requests

BASE_URL = "http://localhost:5000"


def test_endpoint(method, path, data=None, files=None):
    """Test a single endpoint"""
    url = f"{BASE_URL}{path}"

    print(f"\n=== Testing {method} {path} ===")

    try:
        if method == "GET":
            response = requests.get(url)
        elif method == "POST":
            if files:
                response = requests.post(url, data=data, files=files)
            else:
                response = requests.post(url, json=data)
        elif method == "PUT":
            response = requests.put(url, json=data)
        elif method == "PATCH":
            response = requests.patch(url, json=data)
        elif method == "DELETE":
            response = requests.delete(url, json=data)
        else:
            print(f"Unknown method: {method}")
            return

        print(f"Status: {response.status_code}")
        print(f"Headers: {dict(response.headers)}")

        try:
            if response.headers.get("Content-Type", "").startswith("application/json"):
                print(f"Response: {json.dumps(response.json(), indent=2)}")
            else:
                print(f"Response (text): {response.text[:200]}...")
        except:
            print(f"Response (raw): {response.text[:200]}...")

    except Exception as e:
        print(f"Error: {e}")


def main():
    print("Testing Excursion GPT API Endpoints")
    print("=" * 50)

    # Test health endpoint
    test_endpoint("GET", "/health")

    # Test buildings endpoints (from requirements)
    test_endpoint(
        "PUT",
        "/buildings",
        {"position": {"x": 66.3333, "z": 65.4444}, "distance": 1000},
    )

    test_endpoint("PUT", "/buildings/address", {"address": "Main Street 123"})

    # Test upload endpoint
    test_endpoint(
        "POST",
        "/upload",
        files={"file": ("test.glb", b"mock model data", "model/gltf-binary")},
    )

    # Test model endpoints
    test_endpoint(
        "PUT",
        "/model/test_model_001",
        {"position": [10.0, 5.0, 15.0], "rotation": [0.0, 1.5, 0.0], "scale": 1.0},
    )

    test_endpoint("GET", "/model/test_model_001")

    # Test models endpoints
    test_endpoint("PUT", "/models/address", {"address": "Building with model"})

    test_endpoint(
        "PATCH",
        "/models/test_model_001",
        {
            "position": [66.3333, 0.0, 65.4444],
            "rotation": 1.5708,
            "scale": 1.5,
            "polygons": ["polygon_001", "polygon_002"],
            "address": "Updated address",
        },
    )

    # Test tracks endpoints
    test_endpoint("GET", "/tracks/")
    test_endpoint("GET", "/tracks/track_001")
    test_endpoint("POST", "/tracks", {"name": "Test Track"})
    test_endpoint("DELETE", "/tracks/track_999")

    # Test points endpoints
    test_endpoint(
        "POST",
        "/tracks/track_001",
        {
            "name": "Test Point",
            "type": "viewpoint",
            "position": [55.7558, 0.0, 37.6173],
            "rotation": [0.0, 0.0, 0.0],
        },
    )

    test_endpoint(
        "PUT",
        "/tracks/track_001/point_001",
        {
            "name": "Updated Point",
            "type": "info",
            "position": [55.7560, 0.0, 37.6175],
            "rotation": [0.0, 1.57, 0.0],
        },
    )

    test_endpoint("DELETE", "/tracks/track_001/point_001")

    # Also test with /api prefix (old routes)
    print("\n" + "=" * 50)
    print("Testing with /api prefix (old routes)")
    print("=" * 50)

    test_endpoint("GET", "/api/Buildings")
    test_endpoint("GET", "/api/Models")
    test_endpoint("GET", "/api/Tracks")

    # Test specific old routes from Swagger
    test_endpoint(
        "GET",
        "/api/Buildings/around-point/55.7558/37.6173/123e4567-e89b-12d3-a456-426614174000",
    )


if __name__ == "__main__":
    main()
