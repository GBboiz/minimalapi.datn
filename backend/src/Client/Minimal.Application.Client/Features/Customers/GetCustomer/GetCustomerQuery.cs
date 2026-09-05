using MediatR;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.GetCustomer;

public record GetCustomerQuery(Guid Id) : IRequest<CustomerDto?>;
