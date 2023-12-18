using System;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace SpellBound.Core
{
    [CreateAssetMenu(menuName = "SpellBound/Character")]
    public class Character : ScriptableObject
    {
        [SerializeField]
        private int maxHP;
        [SerializeField]
        private int maxMP;
        [SerializeField]
        private int power;

        public Stats MaxHP { get; private set; }
        public Stats MaxMP { get; private set; }
        public int HP { get; private set; }
        public int MP { get; private set; }
        public Stats Power { get; private set; }

        private ISubscriber<int> onHurtSub;
        private IPublisher<int> onHurtPub;

        public void Init()
        {
            this.MaxHP = new Stats(this.maxHP);
            this.MaxMP = new Stats(this.maxMP);
            this.HP = this.MaxHP.Value();
            this.MP = this.MaxMP.Value();
            this.Power = new Stats(this.power);

            (this.onHurtPub, this.onHurtSub) = GlobalMessagePipe.CreateEvent<int>();
        }

        public void Hurt(int damage)
        {
            this.HP -= damage;
            this.onHurtPub.Publish(damage);
        }

        public IDisposable OnHurt(Action<int> handler)
        {
            return this.onHurtSub.Subscribe(handler);
        }

        public void Cast(int mp)
        {
            this.MP -= mp;
        }

        public void Regen(int amount)
        {
            this.MP = Mathf.Min(this.MP + amount, this.MaxMP.Value());
        }
    }
}
