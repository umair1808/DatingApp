import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Member } from '../_models/member';
import { PaginatedResult } from '../_models/pagination';
import { User } from '../_models/user';
import { UserParams } from '../_models/userParams';
import { AccountService } from './account.service';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';


@Injectable({
  providedIn: 'root'
})
export class MembersService {
  baseUrl = environment.apiUrl;
  members: Member[] = [];
  memberCache = new Map();
  userParams: UserParams;
  user: User;

  constructor(private http: HttpClient, private accountService: AccountService) {
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.user = user;
      this.userParams = new UserParams(this.user);
    })
  }

  getUserParams(){
    return this.userParams;
  }

  setUserParams(params: UserParams){
    this.userParams = params;
  }

  resetUserParams(){
    this.userParams = new UserParams(this.user);
    return this.userParams;
  }

  addLike(username: string){
    return this.http.post(this.baseUrl + 'likes/' + username, {}); //an empty object {} is needed for post request
  }

  getLikes(predicate: string, pageNumber: number, pageSize: number){

    let params = getPaginationHeaders(pageNumber, pageSize).append('predicate', predicate);

    return getPaginatedResult<Partial<Member[]>>(this.baseUrl + 'likes', params, this.http);
    
    // return this.http.get<Partial<Member[]>>(this.baseUrl + 'likes?predicate=' + predicate); //query params can be configured via HttpParams
  }

  // getMembers(){
  //   if(this.members.length > 0) return of(this.members);
  //   return this.http.get<Member[]>(this.baseUrl + 'users').pipe( //http.get without any additional params returns us response body
  //     map(members => {
  //       this.members = members;
  //       return members;
  //     })
  //   );
  // }

  getMembers(userParams: UserParams) {
    console.log()
    var response = this.memberCache.get(Object.values(userParams).join("-"));
    if(response){
      return of(response);
    }

    let params = getPaginationHeaders(userParams.pageNumber, userParams.pageSize);

    params = params.append('minAge', userParams.minAge.toString())
            .append('maxAge', userParams.maxAge.toString())
            .append('gender', userParams.gender)
            .append('orderBy', userParams.orderBy)
     
    return getPaginatedResult<Member[]>(this.baseUrl + 'users', params, this.http).pipe(
      map(response => {
        this.memberCache.set(Object.values(userParams).join("-"), response)
        return response;
      })
    )
  }

  getMember(username: string){

    // const member = this.members.find(x => x.userName === username);
    // if(member !== undefined) {
    //   return of(member);
    // }

    const members = [... this.memberCache.values()].reduce((arr, elem) => arr.concat(elem.result), []);
    
    var member = members.find((m: Member) => m.username === username);
    if(member){
      return of(member);
    }
 
    return this.http.get<Member>(this.baseUrl + 'users/'+ username);
  }

  updateMember(member: Member){
    return this.http.put(this.baseUrl + 'users', member).pipe(
      map(() => {
        const index = this.members.indexOf(member);
        this.members[index] = member;
      })
    );
  }

  setMainPhoto(photoId: number){
    return this.http.put(this.baseUrl + 'users/set-main-photo/' + photoId, {});
  }

  deletePhoto(photoId: number){
    return this.http.delete(this.baseUrl + 'users/delete-photo/' + photoId);
  }

}