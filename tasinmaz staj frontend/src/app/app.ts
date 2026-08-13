import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './components/navbar/navbar';
import { ToastContainer } from './components/toast-container/toast-container';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Navbar, ToastContainer],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}