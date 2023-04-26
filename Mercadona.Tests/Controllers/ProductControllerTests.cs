namespace Mercadona.Tests.Controllers
{
    public class ProductControllerTests
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
        public async Task GetImageAsync_DataFound_ShouldReturnOK_WithData()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task GetImageAsync_NoDataFound_ShouldReturnNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task GetImageAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AddProductAsync_NoException_ShouldReturnCreated()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AddProductAsync_ValidationExceptionThrown_ShouldReturnProblem_BadRequest()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AddProductAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            throw new NotImplementedException();
        }
    }
}
