import React, { Component } from 'react';
import { Card, CardHeader, CardBody, Form, FormGroup, Col, InputGroup, Input, InputGroupAddon, Button } from 'reactstrap';

import BootstrapTable from 'react-bootstrap-table-next';
import paginationFactory from 'react-bootstrap-table2-paginator';
import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';
import 'react-bootstrap-table2-paginator/dist/react-bootstrap-table2-paginator.min.css';
import axios from 'axios';
import { authHeader } from '../../_authHeader';
import { Link } from 'react-router-dom';
import { access } from 'fs';


class ClientGroups extends Component {
  constructor(props) {
    super(props);

    this.state = {
      page: 1,
      totalSize: 0,
      sizePerPage: 10,
      clientGroupSearchTerm: '',
      clientGroups: [],
      columns: [{
        dataField: 'id',
        text: 'Id',
        hidden: true,
        headerStyle: (colum, colIndex) => {
          return { width: '50px' };
        }
      },
      {
        dataField: 'firmGroupId',
        text: 'Id',
        sort: true,
        headerStyle: (colum, colIndex) => {
          return { width: '250px' };
        }
      },
      {
        dataField: 'name',
        text: 'Name',
        sort: true,
        formatter: this.renderClientGroupEditLink
      }
      ]
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleTableChange = this.handleTableChange.bind(this);
    this.handleSearchParameterChange = this.handleSearchParameterChange.bind(this);
    this.handleKeyPress = this.handleKeyPress.bind(this);
  }

  componentDidMount() {
    axios.get(`/api/clientgroups?size=${this.state.sizePerPage}&page=1`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ clientGroups: response.data });

          // We need total size as well
          axios.get(`/api/clientgroups/count`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
            .then(response => {
              if (response.status === 200) {
                this.setState({ totalSize: response.data });
              }
            });
        }
      })
      .catch(error => {
        window.location.replace('/');
      });
  }

  renderClientGroupEditLink(cell, row, rowIndex) {
    return (
      <span>
        <Link to={`/directory/clientgroups/${row.id}`} >{cell}</Link>
      </span>
    );
  }

  searchClientGroup = (page, sizePerPage) => {
    const inputValue = this.state.clientGroupSearchTerm;

    // Get updated data
    axios.get(`/api/clientgroups?name=${inputValue}&size=${sizePerPage}&page=${page}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ clientGroups: response.data });

          // We need total size as well
          axios.get(`/api/clientgroups/count?name=${inputValue}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
            .then(response => {
              if (response.status === 200) {
                this.setState({ totalSize: response.data });
              }
            });

        }
      })
      .catch(error => {
        window.location.replace('/');
      });
  }

  handleTableChange = (type, { page, sizePerPage }) => {
    this.setState({ page: page });
    this.setState({ sizePerPage: sizePerPage });

    this.searchClientGroup(page, sizePerPage);
  }

  handleSearch = event => {
    event.preventDefault();
    this.setState({ page: 1 });
    this.searchClientGroup(1, this.state.sizePerPage);
  }

  handleSearchParameterChange = event => {
    this.setState({ clientGroupSearchTerm: event.target.value });
  }

  handleSubmit = event => {
    event.preventDefault();
  }

  handleKeyPress = event => {
    if (event.key === 'Enter') {
      this.searchClientGroup(this.state.page, this.state.sizePerPage);
    }
  }

  render() {
    const { totalSize, sizePerPage, page } = this.state;
    return (
      <div className="animated">
        <Card>
          <CardHeader>
            <i className="icon-menu" />Client Groups{' '}
            <div className="card-header-actions">
              <Link className="card-header-action" to={`/directory/clientgroups/0`}><small className="text-muted">Add New</small></Link> | <Link className="card-header-action" to={`/directory/clientgroups/clientgroupsimport`}><small className="text-muted">Import</small></Link>
            </div>
          </CardHeader>
          <CardBody>
            <Form className="form-horizontal" onSubmit={this.handleSubmit}>
              <FormGroup row>
                <Col md="12">
                  <InputGroup>
                    <Input type="text" id="searchParam" name="searchParam" placeholder="Search" onChange={this.handleSearchParameterChange} onKeyPress={this.handleKeyPress}/>
                    <InputGroupAddon addonType="append">
                      <Button type="button" color="primary" id="search" onClick={this.handleSearch}>Search</Button>
                    </InputGroupAddon>
                  </InputGroup>
                </Col>
              </FormGroup>
            </Form>
            <BootstrapTable remote striped hover keyField='id' data={this.state.clientGroups} columns={this.state.columns} noDataIndication="No client groups for firm have been added" pagination={paginationFactory({ page, sizePerPage, totalSize })} onTableChange={this.handleTableChange} />
          </CardBody>
        </Card>
      </div>
    );
  }

}


export default ClientGroups;
