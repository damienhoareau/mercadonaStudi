namespace Mercadona.Tests.Controllers
{
    public class OfferControllerTests
    {
        [Fact]
        public async Task GetAllAsync_ResultContainsAtLeastOneItem_ShouldReturnOK_WithCorrectItems()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task GetAllAsync_ResultHasNoItem_ShouldReturnNoContent()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task GetAllAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AddOfferAsync_NoException_ShouldReturnCreated()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AddOfferAsync_ValidationExceptionThrown_ShouldReturnProblem_BadRequest()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AddOfferAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            throw new NotImplementedException();
        }
    }
}
