using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Before.Services;

namespace Before.Model
{
    public class Member : Entity
    {
        public Member(string firstName, string lastName, string email)
        {
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Email = email ?? throw new ArgumentNullException(nameof(email));
        }

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public List<Offer> AssignedOffers { get; set; } = new List<Offer>();
        public int NumberOfActiveOffers { get; private set; }


        public async Task<Offer> AssignOffer(OfferType offerType,
            //double dispatch
            IOfferValueCalculator offerValueCalculator,
            CancellationToken cancellationToken)
        {
            // Calculate expiration date
            var dateExpiring = offerType.CalculateExpirationDate();

            // Assign offer
            var value = await offerValueCalculator.CalculateOfferValue(this, offerType, cancellationToken);
            var offer = new Offer(this, offerType, dateExpiring, value);
            
            AssignedOffers.Add(offer);
            NumberOfActiveOffers++;
            return offer;
        }
    }
}