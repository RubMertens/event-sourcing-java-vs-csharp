import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";

@Injectable({
  providedIn: 'root'
})
export class GridService {

  private readonly apiUrl = 'http://localhost:3000';

  constructor(private readonly httpClient: HttpClient) {
  }

  createGrid(width: number, height: number, name: string) {
    return this.httpClient.post<{ id: string }>(`${this.apiUrl}/grids`, {
      'width': width,
      'height': height,
      'name': name
    });
  }

  getGrids() {
    return this.httpClient.get<{ id: string, name: string }[]>(`${this.apiUrl}/grids`);
  }

  getGrid(gridId: number) {
    return this.httpClient.get<{ id: string, name: string, grid: string[][] }>(`${this.apiUrl}/grids/${gridId}`);
  }

  movePixel(gridId: number, x: number, y: number, deltax: number, deltay: number) {
    return this.httpClient.post<{ id: string }>(`${this.apiUrl}/grids/${gridId}/move`, {
      'x': x,
      'y': y,
      'deltax': deltax,
      'deltay': deltay
    });
  }

  colorPixel(gridId: number, x: number, y: number, color: string) {
    return this.httpClient.post<{ id: string }>(`${this.apiUrl}/grids/${gridId}/color`, {
      'x': x,
      'y': y,
      'color': color
    });
  }
}
