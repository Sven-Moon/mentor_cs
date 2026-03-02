using MentoringApp.Api.DTOs.Profiles;

namespace MentoringApp.Api.Mapping
{
	public class MappingProfile : AutoMapper.Profile
	{
		public MappingProfile()
		{
			CreateMap<UpdateProfileDto, ProfileDto>();

			CreateMap<UpdateProfileDto, Models.Profile>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.UserId, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.User, opt => opt.Ignore());
		}
	}
}
