using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SpellBound
{
    public class Crosshair : MonoBehaviour
    {
        [SerializeField]
        private Color overload;
        [SerializeField]
        private float overloadDistance;
        [SerializeField]
        private float rotateAcceleration;

        [Inject]
        private readonly Player player;

        private PlayerController playerController;
        private float heat;
        private Color origin;
        private List<Image> images;
        private List<Transform> sideImages;
        private List<Transform> sides;
        private float rotateSpeed;

        const float MAX_HEAT = 100;
        const float MAX_ROTATE_SPEED = 720;

        void Start()
        {
            this.playerController = this.player.GetComponentInParent<PlayerController>();
            Debug.Assert(this.playerController != null);

            this.heat = 0;
            this.playerController.MainWeapon.Subscribe(_ =>
            {
                this.heat = Mathf.Min(this.heat + 10, Crosshair.MAX_HEAT);
            });
            this.images = GetComponentsInChildren<Image>().ToList();
            Debug.Assert(this.images.Count != 0);
            this.origin = this.images[0].color;

            this.sides = new List<Transform>();
            this.sideImages = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var side = transform.GetChild(i);
                this.sides.Add(side);
                this.sideImages.Add(side.GetChild(0));
            }
            Debug.Assert(this.sides.Count != 0);
            Debug.Assert(this.sideImages.Count == this.sides.Count);
        }

        void Update()
        {
            this.heat = Mathf.Max(0, this.heat - 20 * Time.deltaTime);
            float normHeat = this.heat / Crosshair.MAX_HEAT;

            foreach (var image in this.images)
            {
                image.color = Color.Lerp(this.origin, this.overload, normHeat);
            }
            foreach (var sideImage in this.sideImages)
            {
                sideImage.localPosition = Vector3.up * Mathf.Lerp(0, this.overloadDistance, normHeat);
            }

            if (normHeat > 0.9)
            {
                this.rotateSpeed += this.rotateAcceleration * Time.deltaTime;
            }
            else
            {
                this.rotateSpeed -= this.rotateAcceleration * Time.deltaTime;
            }
            this.rotateSpeed = Mathf.Clamp(this.rotateSpeed, 0, Crosshair.MAX_ROTATE_SPEED);

            foreach (var side in this.sides)
            {
                side.localRotation *= Quaternion.AngleAxis(this.rotateSpeed * Time.deltaTime, Vector3.forward);
            }
        }
    }
}
