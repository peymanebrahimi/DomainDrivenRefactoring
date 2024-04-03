using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Before.Model;
using MediatR;

namespace Before.Services
{
    public interface IOfferValueCalculation
    {
        Task<int> CalculateOfferValue(Member member, OfferType offerType, CancellationToken cancellationToken);
    }

    public class OfferValueCalculation : IOfferValueCalculation
    {
        private readonly HttpClient _httpClient;

        public OfferValueCalculation(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<int> CalculateOfferValue(Member member, OfferType offerType, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(
                $"/calculate-offer-value?email={member.Email}&offerType={offerType.Name}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var value = await JsonSerializer.DeserializeAsync<int>(responseStream, cancellationToken: cancellationToken);
            return value;
        }
    }

    public class AssignOfferHandler : IRequestHandler<AssignOfferRequest>
    {
        private readonly AppDbContext _appDbContext;
        private readonly OfferValueCalculation _offerValueCalculation;

        public AssignOfferHandler(
            AppDbContext appDbContext,
            HttpClient httpClient)
        {
            _appDbContext = appDbContext;
            _offerValueCalculation = new OfferValueCalculation(httpClient);
        }

        public async Task Handle(AssignOfferRequest request, CancellationToken cancellationToken)
        {
            var member = await _appDbContext.Members.FindAsync(request.MemberId, cancellationToken);
            var offerType = await _appDbContext.OfferTypes.FindAsync(request.OfferTypeId, cancellationToken);

            // Calculate offer value
            var value = await _offerValueCalculation.CalculateOfferValue(member, offerType, cancellationToken);

            // Calculate expiration date
            var dateExpiring = CalculateExpirationDate(offerType);

            // Assign offer
            var offer = new Offer
            {
                MemberAssigned = member,
                Type = offerType,
                Value = value,
                DateExpiring = dateExpiring
            };
            member.AssignedOffers.Add(offer);
            member.NumberOfActiveOffers++;

            await _appDbContext.Offers.AddAsync(offer, cancellationToken);

            await _appDbContext.SaveChangesAsync(cancellationToken);
        }

        private static DateTime CalculateExpirationDate(OfferType offerType)
        {
            DateTime dateExpiring;

            switch (offerType.ExpirationType)
            {
                case ExpirationType.Assignment:
                    dateExpiring = DateTime.Today.AddDays(offerType.DaysValid);
                    break;
                case ExpirationType.Fixed:
                    dateExpiring = offerType.BeginDate?.AddDays(offerType.DaysValid)
                                   ?? throw new InvalidOperationException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return dateExpiring;
        }
    }
}