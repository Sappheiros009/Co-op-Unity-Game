using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Primitive and sprite presentation helpers used by the first playable prototype.
    /// These helpers are intentionally replaceable by the approved art pipeline later.
    /// </summary>
    public static class PrototypeVisuals
    {
        private static Shader _litShader;

        public static Material CreateMaterial(string materialName, Color color, bool emission = false)
        {
            if (_litShader == null)
            {
                _litShader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Unlit/Color");
            }

            var material = new Material(_litShader)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.45f);
            }

            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }

            return material;
        }

        public static GameObject CreateCube(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider = true)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            ApplyMaterial(gameObject, material);

            if (!collider)
            {
                var primitiveCollider = gameObject.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    primitiveCollider.enabled = false;
                }
            }

            return gameObject;
        }

        public static GameObject CreateCapsule(
            string objectName,
            Transform parent,
            Vector3 position,
            Color color,
            bool collider = false)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            ApplyMaterial(gameObject, CreateMaterial(objectName + " Material", color));

            if (!collider)
            {
                var primitiveCollider = gameObject.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    primitiveCollider.enabled = false;
                }
            }

            return gameObject;
        }

        public static GameObject CreateSphere(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider = false)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            ApplyMaterial(gameObject, material);

            if (!collider)
            {
                var primitiveCollider = gameObject.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    primitiveCollider.enabled = false;
                }
            }

            return gameObject;
        }

        public static Camera CreateCamera(string objectName, Vector3 position, Vector3 lookAt)
        {
            var cameraObject = new GameObject(objectName);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 150f;
            cameraObject.transform.position = position;
            Face(cameraObject.transform, lookAt);
            return camera;
        }

        public static Camera CreateOrthographicCamera(
            string objectName,
            Vector3 position,
            float orthographicSize,
            Color backgroundColor)
        {
            var cameraObject = new GameObject(objectName);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.identity;
            return camera;
        }

        public static GameObject CreateSprite(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector2 size,
            Color color,
            int sortingOrder = 0)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetWhiteSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        public static void UseWhiteSpriteForEditorPreview(Sprite sprite)
        {
            if (sprite != null)
            {
                _whiteSprite = sprite;
            }
        }

        public static Light CreateDirectionalLight(Transform parent)
        {
            var lightObject = new GameObject("Prototype Sun");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.93f, 0.82f);
            return light;
        }

        public static void Face(Transform target, Vector3 lookAt)
        {
            var direction = lookAt - target.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                target.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private static void ApplyMaterial(GameObject gameObject, Material material)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Sprite _whiteSprite;

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite == null)
            {
                _whiteSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                _whiteSprite.name = "Prototype White Sprite";
            }

            return _whiteSprite;
        }
    }
}
