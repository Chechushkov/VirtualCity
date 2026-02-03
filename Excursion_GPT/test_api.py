import json
import sys
from typing import Any, Dict, List

import requests


class ExcursionGPTAPITester:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url.rstrip("/")
        self.session = requests.Session()
        self.auth_token = None

    def set_auth_token(self, token: str):
        """Set authentication token for requests"""
        self.auth_token = token
        self.session.headers.update({"Authorization": f"Bearer {token}"})

    def clear_auth_token(self):
        """Clear authentication token"""
        self.auth_token = None
        if "Authorization" in self.session.headers:
            del self.session.headers["Authorization"]

    def make_request(
        self, method: str, endpoint: str, data: Dict = None, files: Dict = None
    ) -> Dict:
        """Make HTTP request and return response"""
        url = f"{self.base_url}{endpoint}"

        try:
            if method.upper() == "GET":
                response = self.session.get(url)
            elif method.upper() == "POST":
                if files:
                    response = self.session.post(url, data=data, files=files)
                else:
                    response = self.session.post(url, json=data)
            elif method.upper() == "PUT":
                response = self.session.put(url, json=data)
            elif method.upper() == "PATCH":
                response = self.session.patch(url, json=data)
            elif method.upper() == "DELETE":
                response = self.session.delete(url, json=data)
            else:
                raise ValueError(f"Unsupported method: {method}")

            # Try to parse JSON response
            try:
                result = response.json()
            except:
                result = {"raw_response": response.text}

            result["status_code"] = response.status_code
            result["headers"] = dict(response.headers)

            return result

        except requests.exceptions.RequestException as e:
            return {"error": str(e), "status_code": 0}

    def test_buildings_around_point(self) -> Dict:
        """Test PUT /buildings - Get buildings around point"""
        print("\n=== Testing PUT /buildings ===")

        data = {"position": {"x": 66.3333, "z": 65.4444}, "distance": 1000}

        result = self.make_request("PUT", "/buildings", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_buildings_by_address(self) -> Dict:
        """Test PUT /buildings/address - Get building by address"""
        print("\n=== Testing PUT /buildings/address ===")

        data = {"address": "Main Street 123"}

        result = self.make_request("PUT", "/buildings/address", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_upload_model(self) -> Dict:
        """Test POST /upload - Upload model file"""
        print("\n=== Testing POST /upload ===")

        # Create a dummy file for testing
        files = {
            "file": ("test_model.glb", b"Mock 3D model content", "model/gltf-binary")
        }

        # Note: In real implementation, you might need additional form data
        data = {}

        result = self.make_request("POST", "/upload", data, files)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_update_model_position(self, model_id: str = "test_model_001") -> Dict:
        """Test PUT /model/{model_id} - Update model position"""
        print(f"\n=== Testing PUT /model/{model_id} ===")

        data = {
            "position": [10.0, 5.0, 15.0],
            "rotation": [0.0, 1.5, 0.0],
            "scale": 1.0,
        }

        result = self.make_request("PUT", f"/model/{model_id}", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_get_model_file(self, model_id: str = "test_model_001") -> Dict:
        """Test GET /model/{model_id} - Get model file"""
        print(f"\n=== Testing GET /model/{model_id} ===")

        result = self.make_request("GET", f"/model/{model_id}")
        print(f"Status Code: {result.get('status_code')}")

        # For binary responses, just show headers and size
        if "raw_response" in result:
            print(f"Response size: {len(result['raw_response'])} bytes")
            print(f"Content-Type: {result['headers'].get('Content-Type', 'Unknown')}")
        else:
            print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_get_model_by_address(self) -> Dict:
        """Test PUT /models/address - Get model metadata by address"""
        print("\n=== Testing PUT /models/address ===")

        data = {"address": "Building with model"}

        result = self.make_request("PUT", "/models/address", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_save_model_metadata(self, model_id: str = "test_model_001") -> Dict:
        """Test PATCH /models/{model_id} - Save model metadata"""
        print(f"\n=== Testing PATCH /models/{model_id} ===")

        data = {
            "position": [66.3333, 0.0, 65.4444],
            "rotation": 1.5708,
            "scale": 1.5,
            "polygons": ["polygon_001", "polygon_002"],
            "address": "Updated address",
        }

        result = self.make_request("PATCH", f"/models/{model_id}", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_get_all_tracks(self) -> Dict:
        """Test GET /tracks/ - Get all tracks"""
        print("\n=== Testing GET /tracks/ ===")

        result = self.make_request("GET", "/tracks/")
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_get_track_by_id(self, track_id: str = "track_001") -> Dict:
        """Test GET /tracks/{track_id} - Get track with points"""
        print(f"\n=== Testing GET /tracks/{track_id} ===")

        result = self.make_request("GET", f"/tracks/{track_id}")
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_create_track(self) -> Dict:
        """Test POST /tracks - Create new track"""
        print("\n=== Testing POST /tracks ===")

        data = {"name": "Test Track"}

        result = self.make_request("POST", "/tracks", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_delete_track(self, track_id: str = "track_999") -> Dict:
        """Test DELETE /tracks/{track_id} - Delete track"""
        print(f"\n=== Testing DELETE /tracks/{track_id} ===")

        result = self.make_request("DELETE", f"/tracks/{track_id}")
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_add_point_to_track(self, track_id: str = "track_001") -> Dict:
        """Test POST /tracks/{track_id} - Add point to track"""
        print(f"\n=== Testing POST /tracks/{track_id} ===")

        data = {
            "name": "Test Point",
            "type": "viewpoint",
            "position": [55.7558, 0.0, 37.6173],
            "rotation": [0.0, 0.0, 0.0],
        }

        result = self.make_request("POST", f"/tracks/{track_id}", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_update_point(
        self, track_id: str = "track_001", point_id: str = "point_001"
    ) -> Dict:
        """Test PUT /tracks/{track_id}/{point_id} - Update point"""
        print(f"\n=== Testing PUT /tracks/{track_id}/{point_id} ===")

        data = {
            "name": "Updated Point Name",
            "type": "info",
            "position": [55.7560, 0.0, 37.6175],
            "rotation": [0.0, 1.57, 0.0],
        }

        result = self.make_request("PUT", f"/tracks/{track_id}/{point_id}", data)
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_delete_point(
        self, track_id: str = "track_001", point_id: str = "point_001"
    ) -> Dict:
        """Test DELETE /tracks/{track_id}/{point_id} - Delete point"""
        print(f"\n=== Testing DELETE /tracks/{track_id}/{point_id} ===")

        result = self.make_request("DELETE", f"/tracks/{track_id}/{point_id}")
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def test_health_check(self) -> Dict:
        """Test GET /health - Health check endpoint"""
        print("\n=== Testing GET /health ===")

        result = self.make_request("GET", "/health")
        print(f"Status Code: {result.get('status_code')}")
        print(f"Response: {json.dumps(result, indent=2)}")
        return result

    def run_all_tests(self, with_auth: bool = False):
        """Run all API tests"""
        print("=" * 60)
        print("Excursion GPT API Test Suite")
        print("=" * 60)

        if with_auth:
            print("\n⚠️  Note: Authentication tests require valid JWT token")
            print("Set token using tester.set_auth_token('your_token_here')")

        test_results = {}

        # Test endpoints that don't require authentication first
        test_results["health"] = self.test_health_check()

        # Test building endpoints
        test_results["buildings_around_point"] = self.test_buildings_around_point()
        test_results["buildings_by_address"] = self.test_buildings_by_address()

        # Test model endpoints
        test_results["upload_model"] = self.test_upload_model()
        test_results["update_model_position"] = self.test_update_model_position()
        test_results["get_model_file"] = self.test_get_model_file()
        test_results["get_model_by_address"] = self.test_get_model_by_address()
        test_results["save_model_metadata"] = self.test_save_model_metadata()

        # Test track endpoints
        test_results["get_all_tracks"] = self.test_get_all_tracks()
        test_results["get_track_by_id"] = self.test_get_track_by_id()
        test_results["create_track"] = self.test_create_track()
        test_results["delete_track"] = self.test_delete_track()

        # Test point endpoints
        test_results["add_point_to_track"] = self.test_add_point_to_track()
        test_results["update_point"] = self.test_update_point()
        test_results["delete_point"] = self.test_delete_point()

        # Summary
        print("\n" + "=" * 60)
        print("Test Summary")
        print("=" * 60)

        success_count = 0
        total_count = len(test_results)

        for test_name, result in test_results.items():
            status_code = result.get("status_code", 0)
            if 200 <= status_code < 300:
                status = "✓ PASS"
                success_count += 1
            elif status_code == 401 or status_code == 403:
                status = "⚠️  AUTH REQUIRED"
            else:
                status = "✗ FAIL"

            print(f"{test_name:30} {status:15} Status: {status_code}")

        print(
            f"\nSuccess Rate: {success_count}/{total_count} ({success_count / total_count * 100:.1f}%)"
        )

        return test_results


def main():
    # Parse command line arguments
    import argparse

    parser = argparse.ArgumentParser(description="Test Excursion GPT API")
    parser.add_argument(
        "--url", default="http://localhost:5000", help="Base URL of the API"
    )
    parser.add_argument("--token", help="JWT token for authentication")
    parser.add_argument("--test", help="Run specific test")
    args = parser.parse_args()

    # Create tester instance
    tester = ExcursionGPTAPITester(base_url=args.url)

    # Set authentication token if provided
    if args.token:
        tester.set_auth_token(args.token)
        print(f"Using authentication token: {args.token[:20]}...")

    # Run specific test or all tests
    if args.test:
        # Map test names to methods
        test_methods = {
            "health": tester.test_health_check,
            "buildings_around_point": tester.test_buildings_around_point,
            "buildings_by_address": tester.test_buildings_by_address,
            "upload_model": tester.test_upload_model,
            "update_model_position": tester.test_update_model_position,
            "get_model_file": tester.test_get_model_file,
            "get_model_by_address": tester.test_get_model_by_address,
            "save_model_metadata": tester.test_save_model_metadata,
            "get_all_tracks": tester.test_get_all_tracks,
            "get_track_by_id": tester.test_get_track_by_id,
            "create_track": tester.test_create_track,
            "delete_track": tester.test_delete_track,
            "add_point_to_track": tester.test_add_point_to_track,
            "update_point": tester.test_update_point,
            "delete_point": tester.test_delete_point,
        }

        if args.test in test_methods:
            test_methods[args.test]()
        else:
            print(f"Unknown test: {args.test}")
            print(f"Available tests: {', '.join(test_methods.keys())}")
            sys.exit(1)
    else:
        # Run all tests
        tester.run_all_tests(with_auth=bool(args.token))


if __name__ == "__main__":
    main()
