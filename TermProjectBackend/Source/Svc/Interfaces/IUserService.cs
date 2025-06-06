﻿using TermProjectBackend.Models;
using TermProjectBackend.Models.Dto;

namespace TermProjectBackend.Source.Svc.Interfaces
{
    public interface IUserService
    {
        bool IsUserUnique(string userName);

        public int getUserId(User user);

        public int getUserIdByName(string userName);
        public Task<LoginResponseDTO> Login(LoginRequestDTO loginReguestDTO);

        public User Register(RegisterationRequestDTO reqisterationRequestDTO);

        public User GetUserInformationById(int id);

        public void DeleteAccount(int id);

        public List<string> GetAllUserNames();

    }
}
