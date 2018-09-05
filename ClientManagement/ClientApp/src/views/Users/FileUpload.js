import React, { Component } from 'react';
import { Input } from 'reactstrap';
import axios from 'axios';
import { authHeader } from '../../_authHeader';

class FileUpload extends Component {
  constructor(props) {
    super(props);
  }

  handleFileChange = (e) => {
    const concat = require("concat-stream");
    let a = e.target.files[0];
    const fd = new FormData();
    fd.append("file", a, 'profile.jpg');
    fd.append("name", "LUKE IS AWESOME!");
    alert(a.size);

    axios.post("api/userprofiles/update", fd, {
      headers: { ...authHeader() }
    })
      .catch(error => {
        console.log(error.response);
      });
  }


  render() {
    return (
      <Input type="file" accept="image/jpeg" placeholder="Profile image" onChange={this.handleFileChange} />
      );
  }
}

export default FileUpload;
