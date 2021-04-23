import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Photo } from '../_models/photo';
import { User } from '../_models/user';

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  baseUrl = environment.apiUrl

  constructor(private http: HttpClient) { }

  getUserWithRoles(){
    return this.http.get<Partial<User[]>>(this.baseUrl + 'admin/users-with-roles');
  }

  updateUserRoles(username: string, roles: string[]){
    return this.http.post(this.baseUrl + 'admin/edit-roles/' + username + '?roles=' + roles, {}); //post requires {} object
  }

  getUnapprovedPhotos(){
    return this.http.get<Partial<Photo[]>>(this.baseUrl + 'admin/photos-to-moderate');
  }

  approvePhoto(id: Number){
    return this.http.put(this.baseUrl + 'admin/approve-photo/' + id ,{});
  }
}
