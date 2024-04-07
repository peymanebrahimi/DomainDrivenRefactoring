using System;

namespace Before.Model
{
    public class Offer : Entity
    {
        public Offer(Member memberAssigned, OfferType type, DateTime dateExpiring, int value)
        {
            MemberAssigned = memberAssigned ?? throw new ArgumentNullException(nameof(memberAssigned));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            DateExpiring = dateExpiring;
            Value = value;
        }

        public Member MemberAssigned { get; private set; }
        public OfferType Type { get; private set; }
        public DateTime DateExpiring { get; private set; }
        public int Value { get; private set; }
    }
}