using MentoringApp.Api.DTOs.Profiles;

namespace MentoringApp.Api.Mapping
{
	public class MappingProfile : AutoMapper.Profile
	{
		public MappingProfile()
		{
			CreateMap<UpdateProfileDto, ProfileDto>();
		}
	}
}
