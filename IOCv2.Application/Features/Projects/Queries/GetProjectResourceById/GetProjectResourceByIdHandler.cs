using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectResourceById
{
    public class GetProjectResourceByIdHandler : IRequestHandler<GetProjectResourceByIdQuery., Result<GetProjectResourceByIdResponse>>
    {
    }
}
