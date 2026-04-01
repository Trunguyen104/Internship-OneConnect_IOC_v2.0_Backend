//using IOCv2.Application.Interfaces;
//using IOCv2.Domain.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase
//{
//    public class GetEnterpriseInterPhaseHandler : MediatR.IRequestHandler<GetEnterpriseInterPhaseQuery, GetEnterpriseInterPhaseResponse>
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly IMessageService _messageService;
//        private readonly ICurrentUserService _currentUserService;

//        public GetEnterpriseInterPhaseHandler(IUnitOfWork unitOfWork, IMessageService messageService, ICurrentUserService currentUserService)
//        {
//            _unitOfWork = unitOfWork;
//            _messageService = messageService;
//            _currentUserService = currentUserService;
//        }

//        public async Task<GetEnterpriseInterPhaseResponse> Handle(GetEnterpriseInterPhaseQuery request, CancellationToken cancellationToken)
//        {
//            // Normalize search term
//            var search = request?.SearchTerm?.Trim();
//            var hasSearch = !string.IsNullOrWhiteSpace(search);
//            if (hasSearch)
//            {
//                search = search!.ToLowerInvariant();
//            }

//            // Queryables
//            var enterprisesQ = _unitOfWork.Repository<Enterprise>().Query().AsQueryable();
//            var phasesQ = _unitOfWork.Repository<InternshipPhase>().Query().AsQueryable();

//            // Join and filter
//            var query = from e in enterprisesQ
//                        join p in phasesQ on e.EnterpriseId equals p.EnterpriseId
//                        select new
//                        {
//                            EnterpriseId = e.EnterpriseId,
//                            EnterpriseName = e.Name,
//                            PhaseId = p.PhaseId,
//                            PhaseName = p.Name,
//                            MajorFields = (p.MajorFields ?? e.MajorFields) ?? string.Empty, // prefer phase, fallback enterprise
//                            TotalSlots = (int?)(p.TotalSlots ?? 0),
//                            TakenSlots = (int?)(p.TakenSlots ?? 0)
//                        };

//            if (hasSearch)
//            {
//                query = query.Where(x =>
//                    (x.EnterpriseName ?? string.Empty).ToLower().Contains(search)
//                    || (x.PhaseName ?? string.Empty).ToLower().Contains(search));
//            }

//            // Materialize and compute remaining slots
//            var list = await Task.Run(() =>
//                query
//                .AsEnumerable()
//                .Select(x =>
//                {
//                    var total = x.TotalSlots ?? 0;
//                    var taken = x.TakenSlots ?? 0;
//                    var remaining = Math.Max(0, total - taken);
//                    var majorFields = string.Join(", ", (x.MajorFields ?? string.Empty)
//                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
//                        .Select(s => s.Trim())
//                        .Where(s => !string.IsNullOrEmpty(s)));

//                    return new EnterprisePhaseItem
//                    {
//                        EnterpriseId = x.EnterpriseId,
//                        EnterpriseName = x.EnterpriseName ?? string.Empty,
//                        PhaseId = x.PhaseId,
//                        PhaseName = x.PhaseName ?? string.Empty,
//                        MajorFields = majorFields,
//                        RemainingSlots = remaining,
//                        TotalSlots = total
//                    };
//                })
//                .OrderBy(x => x.EnterpriseName)
//                .ThenBy(x => x.PhaseName)
//                .ToList()
//            , cancellationToken).ConfigureAwait(false);

//            // Build response
//            var response = new GetEnterpriseInterPhaseResponse
//            {
//                Items = list
//            };

//            return response;
//        }

//        // Local DTO used by handler and returned in response.Items
//        // This must match or be compatible with the shape expected by UI/response consumer.
//        public class EnterprisePhaseItem
//        {
//            public Guid EnterpriseId { get; set; }
//            public Guid PhaseId { get; set; }
//            public string EnterpriseName { get; set; } = string.Empty;
//            public string PhaseName { get; set; } = string.Empty;
//            public string MajorFields { get; set; } = string.Empty;
//            public int RemainingSlots { get; set; }
//            public int TotalSlots { get; set; }

//            public override string ToString()
//            {
//                // Example: "Công ty ABC — Summer 2025 — CNTT, Kế toán — Còn 3/10 slot"
//                return $"{EnterpriseName} — {PhaseName} — {MajorFields} — Còn {RemainingSlots}/{TotalSlots} slot";
//            }
//        }
//    }
//}
