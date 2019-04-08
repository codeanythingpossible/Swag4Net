using System;
using ClientsForSwagger.RestClient.Results.DiscriminatedUnions;
using Xunit;

namespace GeneratedClientTests
{
    public class UnionTests
    {
        [Fact]
        public void Match2CasesTest()
        {
            var union = new DiscriminatedUnion<string, bool>("ok");

            Assert.True(union.Match(
                s =>  true,
                b => false
            ));
        }

        [Fact]
        public void Match3CasesTest()
        {
            var union = new DiscriminatedUnion<string, bool, int>(true);

            var fail = true;

            var r = union.Match(
                s => fail = true,
                b => fail = false,
                i => fail = true
            );
            
            Assert.False(fail);
            Assert.False(r);
        }
        
        [Fact]
        public void Match4CasesTest()
        {
            var union = new DiscriminatedUnion<string, bool, int, long>(8L);

            Assert.Throws<Exception>(() =>
            {
                union.Match(
                    s => true,
                    b => false
                );
            });

            Assert.True(union.Match(
                s =>  false,
                b => false,
                i => false,
                l => true
            ));
        }        
        
    }
}