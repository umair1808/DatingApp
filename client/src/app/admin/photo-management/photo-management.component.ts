import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { Photo } from 'src/app/_models/photo';
import { AdminService } from 'src/app/_services/admin.service';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {

  photos:Partial<Photo[]>

  constructor(private adminService: AdminService, private toastr: ToastrService) { }

  ngOnInit(): void {
    this.getUnapprovedPhotos()
  }

  getUnapprovedPhotos(){
    this.adminService.getUnapprovedPhotos().subscribe(photos => {
      this.photos = photos
    })
  }

  approvePhoto(id: number){
    this.adminService.approvePhoto(id).subscribe((photo: Partial<Photo>) => {
      if(photo){
        this.photos = this.photos.filter(p => p.id !== photo.id)
        this.toastr.success("Photo Approved Successfully");
      }
    });
  }

}
