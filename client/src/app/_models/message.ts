export interface Message {
    id: number;
    senderId: number;
    senderUsername: string;
    senderPhotoUrl: string;
    recipientId: number;
    recipentUsername: string;
    recipentPhotoUrl: string;
    content: string;
    dateRead?: Date;
    messageSent: Date;
}